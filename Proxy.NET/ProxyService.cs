using System.Net;
using System.Net.Sockets;

public class ProxyService(IUrlMatcher urlMatcher)
{
    private readonly IUrlMatcher _urlMatcher = urlMatcher;

    public async Task RunAsync()
    {
        var listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine($"HTTPS Proxy.NET server started at ip {listener.Server.LocalEndPoint}");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
            _ = HandleClientRequestAsync(client);
        }
    }

    private async Task HandleClientRequestAsync(TcpClient client)
    {
        try
        {
            await using NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream);
            await using var writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            string? requestLine = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(requestLine))
                return;

            string[] parts = requestLine.Split(' ');
            string method = parts[0];
            string url = parts[1];
            Uri uri = new Uri("https://" + url);
            Console.WriteLine($"Request for {uri.Host}");

            if (method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("CONNECT FOUND");
                if (_urlMatcher.IsMatch(url))
                {
                    await writer.WriteLineAsync("HTTP/1.1 200 Connection established");
                    await writer.WriteLineAsync();
                    await TunnelHttpsAsync(client, uri.Host, uri.Port);
                }
                else
                {
                    await writer.WriteLineAsync("HTTP/1.1 403 Forbidden");
                    await writer.WriteLineAsync();
                }
            }
            else if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                if (_urlMatcher.IsMatch(uri.Host))
                {
                    Console.WriteLine($"Forwarding request for {uri.Host}");
                    await ForwardHttpRequestAsync(client, uri, requestLine, reader);
                }
                else
                {
                    await writer.WriteLineAsync("HTTP/1.1 403 Forbidden");
                    await writer.WriteLineAsync();
                }
            }
            else
            {
                await writer.WriteLineAsync("HTTP/1.1 405 Method Not Allowed");
                await writer.WriteLineAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar a requisição: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private async Task TunnelHttpsAsync(TcpClient client, string host, int port)
    {
        try
        {
            using TcpClient remoteClient = new TcpClient(host, port);
            await using NetworkStream remoteStream = remoteClient.GetStream();
            NetworkStream clientStream = client.GetStream();

            var clientToServer = clientStream.CopyToAsync(remoteStream);
            var serverToClient = remoteStream.CopyToAsync(clientStream);

            await Task.WhenAny(clientToServer, serverToClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao tunelar tráfego HTTPS: {ex.Message}");
        }
    }
    
    private async Task ForwardHttpRequestAsync(TcpClient client, Uri uri, string requestLine, StreamReader reader)
    {
        try
        {
            using var remoteClient = new TcpClient(uri.Host, uri.Port == -1 ? 80 : uri.Port);
            using NetworkStream remoteStream = remoteClient.GetStream();
            using var remoteWriter = new StreamWriter(remoteStream) { AutoFlush = true };
            using var remoteReader = new StreamReader(remoteStream);

            // Reencaminhar a linha de requisição e os cabeçalhos para o servidor de destino
            await remoteWriter.WriteLineAsync(requestLine);

            string line;
            while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync()))
            {
                await remoteWriter.WriteLineAsync(line);
            }
            await remoteWriter.WriteLineAsync();

            // Encaminha a resposta do servidor de volta para o cliente
            await remoteReader.BaseStream.CopyToAsync(client.GetStream());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao encaminhar a requisição HTTP: {ex.Message}");
        }
    }
}
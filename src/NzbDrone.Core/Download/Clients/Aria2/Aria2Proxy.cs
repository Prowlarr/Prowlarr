using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using CookComputing.XmlRpc;
using NLog;

namespace NzbDrone.Core.Download.Clients.Aria2
{
    public interface IAria2Proxy
    {
        string GetVersion(Aria2Settings settings);
        string AddUri(Aria2Settings settings, string magnet);
        string AddTorrent(Aria2Settings settings, byte[] torrent);
        Aria2Status GetFromGID(Aria2Settings settings, string gid);
    }

    public interface IAria2 : IXmlRpcProxy
    {
        [XmlRpcMethod("aria2.getVersion")]
        Aria2Version GetVersion(string token);

        [XmlRpcMethod("aria2.addUri")]
        string AddUri(string token, string[] uri);

        [XmlRpcMethod("aria2.addTorrent")]
        string AddTorrent(string token, byte[] torrent);

        [XmlRpcMethod("aria2.forceRemove")]
        string Remove(string token, string gid);

        [XmlRpcMethod("aria2.tellStatus")]
        Aria2Status GetFromGid(string token, string gid);

        [XmlRpcMethod("aria2.getGlobalOption")]
        XmlRpcStruct GetGlobalOption(string token);

        [XmlRpcMethod("aria2.tellActive")]
        Aria2Status[] GetActives(string token);

        [XmlRpcMethod("aria2.tellWaiting")]
        Aria2Status[] GetWaitings(string token, int offset, int num);

        [XmlRpcMethod("aria2.tellStopped")]
        Aria2Status[] GetStoppeds(string token, int offset, int num);
    }

    public class Aria2Proxy : IAria2Proxy
    {
        private readonly Logger _logger;

        public Aria2Proxy(Logger logger)
        {
            _logger = logger;
        }

        private string GetToken(Aria2Settings settings)
        {
            return $"token:{settings?.SecretToken}";
        }

        private string GetURL(Aria2Settings settings)
        {
            return $"http{(settings.UseSsl ? "s" : "")}://{settings.Host}:{settings.Port}{settings.RpcPath}";
        }

        public string GetVersion(Aria2Settings settings)
        {
            _logger.Debug("> aria2.getVersion");

            var client = BuildClient(settings);
            var version = ExecuteRequest(() => client.GetVersion(GetToken(settings)));

            _logger.Debug("< aria2.getVersion");

            return version.Version;
        }

        public Aria2Status GetFromGID(Aria2Settings settings, string gid)
        {
            _logger.Debug("> aria2.tellStatus");

            var client = BuildClient(settings);
            var found = ExecuteRequest(() => client.GetFromGid(GetToken(settings), gid));

            _logger.Debug("< aria2.tellStatus");

            return found;
        }

        public string AddUri(Aria2Settings settings, string magnet)
        {
            _logger.Debug("> aria2.addUri");

            var client = BuildClient(settings);
            var gid = ExecuteRequest(() => client.AddUri(GetToken(settings), new[] { magnet }));

            _logger.Debug("< aria2.addUri");

            return gid;
        }

        public string AddTorrent(Aria2Settings settings, byte[] torrent)
        {
            _logger.Debug("> aria2.addTorrent");

            var client = BuildClient(settings);
            var gid = ExecuteRequest(() => client.AddTorrent(GetToken(settings), torrent));

            _logger.Debug("< aria2.addTorrent");

            return gid;
        }

        private IAria2 BuildClient(Aria2Settings settings)
        {
            var client = XmlRpcProxyGen.Create<IAria2>();
            client.Url = GetURL(settings);

            return client;
        }

        private T ExecuteRequest<T>(Func<T> task)
        {
            try
            {
                return task();
            }
            catch (XmlRpcServerException ex)
            {
                throw new DownloadClientException("Unable to connect to aria2, please check your settings", ex);
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.TrustFailure)
                {
                    throw new DownloadClientUnavailableException("Unable to connect to aria2, certificate validation failed.", ex);
                }

                throw new DownloadClientUnavailableException("Unable to connect to aria2, please check your settings", ex);
            }
        }
    }
}

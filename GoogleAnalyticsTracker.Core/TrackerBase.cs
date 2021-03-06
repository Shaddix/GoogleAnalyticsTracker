﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoogleAnalyticsTracker.Core.Interface;

namespace GoogleAnalyticsTracker.Core
{
    public partial class TrackerBase : IDisposable
    {
        public const string TrackingAccountConfigurationKey = "GoogleAnalyticsTracker.TrackingAccount";
        public const string TrackingDomainConfigurationKey = "GoogleAnalyticsTracker.TrackingDomain";

        const string BeaconUrl = "http://www.google-analytics.com/collect";
        const string BeaconUrlSsl = "https://ssl.google-analytics.com/collect";

        public string TrackingAccount { get; set; }
        public string TrackingDomain { get; set; }
        public IAnalyticsSession AnalyticsSession { get; set; }

        public string Hostname { get; set; }
        public string Language { get; set; }
        public string UserAgent { get; set; }
        public string CharacterSet { get; set; }

        public bool ThrowOnErrors { get; set; }
        public bool UseSsl { get; set; }

        private static HttpClient _httpClient = new HttpClient();

        public TrackerBase(string trackingAccount, string trackingDomain, ITrackerEnvironment trackerEnvironment)
            : this(trackingAccount, trackingDomain, new AnalyticsSession(), trackerEnvironment)
        {
        }

        public TrackerBase(string trackingAccount, string trackingDomain, IAnalyticsSession analyticsSession, ITrackerEnvironment trackerEnvironment)
        {
            TrackingAccount = trackingAccount;
            TrackingDomain = trackingDomain;
            AnalyticsSession = analyticsSession;

            Hostname = trackerEnvironment.Hostname;
            Language = "en";
            UserAgent = string.Format("GoogleAnalyticsTracker/3.0 ({0}; {1}; {2})", trackerEnvironment.OsPlatform, trackerEnvironment.OsVersion, trackerEnvironment.OsVersionString);

            InitializeCharset();
        }

        private void InitializeCharset()
        {
            CharacterSet = "UTF-8";
        }

        private async Task<TrackingResult> RequestUrlAsync(string url, IDictionary<string, string> parameters, string userAgent = null)
        {
            // Create GET string
            var data = new StringBuilder();
            foreach (var parameter in parameters)
            {
                data.Append(string.Format("{0}={1}&", parameter.Key, Uri.EscapeDataString(parameter.Value)));
            }

            // Build TrackingResult
            var returnValue = new TrackingResult
            {
                Url = url,
                Parameters = parameters
            };

            // Determine referer URL
            var referer = string.Format("http://{0}/", TrackingDomain);
            if (parameters.ContainsKey("ReferralUrl"))
            {
                referer = parameters["ReferralUrl"];
            }

            // Create request
            HttpRequestMessage requestMessage;
            try
            {
                requestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format("{0}?{1}", url, data));
                requestMessage.Headers.Add("Referer", referer);
                requestMessage.Headers.Add("User-Agent", userAgent ?? UserAgent);
            }
            catch (Exception ex)
            {
                if (ThrowOnErrors)
                    throw;

                returnValue.Success = false;
                returnValue.Exception = ex;
                return returnValue;
            }

            // Perform request
            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.SendAsync(requestMessage);
                returnValue.Success = true;
            }
            catch (Exception ex)
            {
                if (ThrowOnErrors)
                    throw;
                else
                {
                    returnValue.Success = false;
                    returnValue.Exception = ex;
                }
            }
            finally
            {
                if (response != null)
                    response.Dispose();
            }

            return returnValue;
        }

        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

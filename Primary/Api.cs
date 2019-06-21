﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Primary.Data;
using ServiceStack;

namespace Primary
{
    public class Api
    {
        public static Uri ProductionEndpoint => new Uri("https://api.primary.com.ar");
        public static Uri DemoEndpoint => new Uri("http://pbcp-remarket.cloud.primary.com.ar");
        
        public Api(Uri baseUri)
        {
            BaseUri = baseUri;
        }

        public Uri BaseUri { get; }

        #region Login

        public string AccessToken { get; set; }

        public async Task Login(string username, string password)
        {
            var uri = new Uri(BaseUri, "/auth/getToken");
            
            await uri.ToString().PostToUrlAsync(null, "*/*", 
                                                request =>
                                                {
                                                    request.Headers.Add("X-Username", username);
                                                    request.Headers.Add("X-Password", password);
                                                },
                                                response =>
                                                {
                                                    AccessToken = response.Headers["X-Auth-Token"];
                                                }
            );
        }

        public const string DemoUsername = "naicigam2046";
        public const string DemoPassword = "nczhmL9@";

        #endregion

        #region Instruments information

        public async Task< IEnumerable<Instrument> > GetAllInstruments()
        {
            var uri = new Uri(BaseUri, "/rest/instruments/all");
            var response = await uri.ToString().GetJsonFromUrlAsync( request =>
            {
                request.Headers.Add("X-Auth-Token", AccessToken);
            });
            
            var data = JsonConvert.DeserializeObject<GetAllInstrumentsResponse>(response);
            return data.Instruments.Select(i => i.InstrumentId);
        }

        private class GetAllInstrumentsResponse
        {
            public class InstrumentEntry
            {
                [JsonProperty("instrumentId")]
                public Instrument InstrumentId { get; set; }
            }

            [JsonProperty("instruments")]
            public List<InstrumentEntry> Instruments { get; set; }
        }

        #endregion

        #region Historical data
        
        public async Task< IEnumerable<Trade> > GetHistoricalTrades(Instrument instrument, 
                                                                    DateTime dateFrom, 
                                                                    DateTime dateTo)
        {
            var uri = new Uri(BaseUri, "/rest/data/getTrades");
            //marketId=ROFX&symbol=DOFeb19&dateFrom=2019-01-01&dateTo=2019-01-10

            var response = await uri.ToString()
                                    .AddQueryParam("marketId", instrument.Market)
                                    .AddQueryParam("symbol", instrument.Symbol)
                                    .AddQueryParam("dateFrom", dateFrom.ToString("yyyy-MM-dd"))
                                    .AddQueryParam("dateTo", dateTo.ToString("yyyy-MM-dd"))
                                    .GetJsonFromUrlAsync( request =>
                                    {
                                        request.Headers.Add("X-Auth-Token", AccessToken);
                                    });
            
            var data = JsonConvert.DeserializeObject<GetTradesResponse>(response);
            return data.Trades;
        }

        private class GetTradesResponse
        {
            [JsonProperty("trades")]
            public List<Trade> Trades { get; set; }
        }

        #endregion
        
        public MarketDataWebSocket CreateSocket(IEnumerable<Instrument> instruments, 
                                                IEnumerable<Entry> entries,
                                                uint level, uint depth
        )
        {
            var url = new UriBuilder(BaseUri)
            {
                Scheme = "ws"
            };
            return new MarketDataWebSocket(instruments, entries, level, depth, url.Uri, AccessToken);
        }
    }
}
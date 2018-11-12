﻿using IEXTrading.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace IEXTrading.Infrastructure.IEXTradingHandler
{
    public class IEXHandler
    {
        static string BASE_URL = "https://api.iextrading.com/1.0/"; //This is the base URL, method specific URL is appended to this.
        HttpClient httpClient;

        public IEXHandler()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        /****
         * Calls the IEX reference API to get the list of symbols. 
        ****/
        public List<Company> GetSymbols()
        {
            string IEXTrading_API_PATH = BASE_URL + "ref-data/symbols";
            string companyList = "";

            List<Company> companies = null;

            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                companyList = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            if (!companyList.Equals(""))
            {
                companies = JsonConvert.DeserializeObject<List<Company>>(companyList);
                //companies = companies.GetRange(0, 9);
            }
            return companies;
        }


        public List<Quote> GetQuotes(List<Company> companies)
        {
            string symbols = "";

            List<Quote> quoteList = new List<Quote>();
            Dictionary<string, Dictionary<string, Quote>> quoteDict = null;
            int batchStart = 0;
            int batchEnd = 100;
            int stepCount = 100;
            List<Company> batchCompanies = null;
            List<CompanyStrategyValue> companyStrategyValueList = new List<CompanyStrategyValue>();
            while (batchEnd <= companies.Count)
            {
                int count = 0;
                symbols = "";
                batchCompanies = new List<Company>();
                batchCompanies = companies.GetRange(batchStart, stepCount);
                foreach (var company in batchCompanies)
                {
                    count++;
                    symbols = symbols + company.symbol + ",";
                }


                string IEXTrading_API_PATH = BASE_URL + "stock/market/batch?symbols=" + symbols + "&types=quote";
                string quoteResponse = "";

                //Dictionary<string, Quote> quotesDict = new Dictionary<string, Quote>();
                HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    quoteResponse = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                quoteDict = new Dictionary<string, Dictionary<string, Quote>>();
                if (!string.IsNullOrEmpty(quoteResponse))
                {
                    quoteDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Quote>>>(quoteResponse);
                }

                foreach (var quoteItem in quoteDict)
                {

                    foreach (var quote in quoteItem.Value)
                    {
                        if (quote.Value != null)
                        {
                            quoteList.Add(quote.Value);
                        }
                    }
                }


                batchStart = batchEnd;
                batchEnd = batchEnd + 100;
                if (batchEnd > companies.Count)
                {
                    stepCount = batchEnd - companies.Count;
                }
            }
            //CompanyStrategyValue companyStrategyValue = null;

            //foreach (var quote in quoteList)
            //{
            //    companyStrategyValue = new CompanyStrategyValue();
            //    companyStrategyValue.symbol = quote.symbol;
            //    if ((quote.week52High - quote.week52Low) != 0)
            //    {
            //        companyStrategyValue.companyValue = ((quote.close - quote.week52Low) / (quote.week52High - quote.week52Low));
            //    }
            //    companyStrategyValueList.Add(companyStrategyValue);
            //}

            //return companyStrategyValueList.OrderByDescending(a => a.companyValue).Take(5).ToList();
            return quoteList;
        }

        public List<CompanyStrategyValue> GetTop5Picks(List<Company> companies)
        {
            List<Quote> quoteList = new List<Quote>();
            CompanyStrategyValue companyStrategyValue = null;
            quoteList = GetQuotes(companies);
            List<CompanyStrategyValue> companyStrategyValueList = new List<CompanyStrategyValue>();
            foreach (var quote in quoteList)
            {
                companyStrategyValue = new CompanyStrategyValue();
                companyStrategyValue.symbol = quote.symbol;
                if ((quote.week52High - quote.week52Low) != 0)
                {
                    companyStrategyValue.companyValue = ((quote.close - quote.week52Low) / (quote.week52High - quote.week52Low));
                }
                companyStrategyValueList.Add(companyStrategyValue);
            }
            
            return companyStrategyValueList.OrderByDescending(a => a.companyValue).Take(5).ToList();
        }

        /****
         * Calls the IEX stock API to get 1 year's chart for the supplied symbol. 
        ****/
        public List<Equity> GetChart(string symbol)
        {
            //Using the format method.
            //string IEXTrading_API_PATH = BASE_URL + "stock/{0}/batch?types=chart&range=1y";
            //IEXTrading_API_PATH = string.Format(IEXTrading_API_PATH, symbol);

            string IEXTrading_API_PATH = BASE_URL + "stock/" + symbol + "/batch?types=chart&range=1y";

            string charts = "";
            List<Equity> Equities = new List<Equity>();
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                charts = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            if (!charts.Equals(""))
            {
                ChartRoot root = JsonConvert.DeserializeObject<ChartRoot>(charts, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                Equities = root.chart.ToList();
            }
            //make sure to add the symbol the chart
            foreach (Equity Equity in Equities)
            {
                Equity.symbol = symbol;
            }

            return Equities;
        }
    }
}

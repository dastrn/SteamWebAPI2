﻿using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SteamWebAPI2
{
    internal abstract class SteamWebRequest
    {
        private SteamWebRequestParameter developerKey;

        public SteamWebRequest(SteamWebRequestParameter developerKey)
        {
            // we assert here because SteamWebRequest is never constructed by the application, instead it is constructed by SteamWebSession (controlled by this library)
            Debug.Assert(developerKey != null);

            // we assert here because the name is never determined by the caller and should never be null or empty
            Debug.Assert(!String.IsNullOrEmpty(developerKey.Name));

            if (String.IsNullOrEmpty(developerKey.Value))
            {
                throw new ArgumentNullException("Steam Web API developer key value cannot be null or empty.");
            }

            this.developerKey = developerKey;
        }

        protected async Task<T> GetJsonAsync<T>(string interfaceName, string methodName, int methodVersion)
        {
            Debug.Assert(!String.IsNullOrEmpty(interfaceName));
            Debug.Assert(!String.IsNullOrEmpty(methodName));
            Debug.Assert(methodVersion > 0);

            return await GetJsonAsync<T>(interfaceName, methodName, methodVersion, null);
        }

        protected async Task<T> GetJsonAsync<T>(string interfaceName, string methodName, int methodVersion, IList<SteamWebRequestParameter> parameters)
        {
            Debug.Assert(!String.IsNullOrEmpty(interfaceName));
            Debug.Assert(!String.IsNullOrEmpty(methodName));
            Debug.Assert(methodVersion > 0);

            if (parameters == null)
            {
                parameters = new List<SteamWebRequestParameter>();
            }

            parameters.Insert(0, developerKey);

            string command = BuildRequestCommand(interfaceName, methodName, methodVersion, parameters);

            HttpClient httpClient = new HttpClient();
            string response = await httpClient.GetStringAsync(command);
            response = response.Replace("\n", "");
            response = response.Replace("\t", "");

            var deserializedResult = JsonConvert.DeserializeObject<T>(response);
            return deserializedResult;
        }

        /// <summary>
        /// Takes values and returns a command string that can be sent to the Steam Web API remote address.
        /// Example of a built command: https://api.steampowered.com/ISteamWebAPIUtil/GetSupportedAPIList/v0001/?key=8A05823474AB641D684EBD95AB5F2E47
        /// </summary>
        /// <param name="interfaceName">Example: ISteamWebAPIUtil</param>
        /// <param name="methodName">Example: GetSupportedAPIList</param>
        /// <param name="methodVersion">Example: 1</param>
        /// <param name="parameters">Example: { key: 8A05823474AB641D684EBD95AB5F2E47 } </param>
        /// <returns></returns>
        private string BuildRequestCommand(string interfaceName, string methodName, int methodVersion, IList<SteamWebRequestParameter> parameters)
        {
            Debug.Assert(!String.IsNullOrEmpty(interfaceName));
            Debug.Assert(!String.IsNullOrEmpty(methodName));
            Debug.Assert(methodVersion > 0);

            string steamWebApiBaseUrl = ConfigurationManager.AppSettings["steamWebApiBaseUrl"];

            if (String.IsNullOrEmpty(steamWebApiBaseUrl))
            {
                throw new ConfigurationErrorsException("You must include a 'steamWebApiBaseUrl' value in the AppSettings section of your app.config or web.config. A common value is 'https://api.steampowered.com'. It is up to the application to manage this value in case the URL changes in the future.");
            }

            string command = String.Format("{0}/{1}/{2}/v{3}/", steamWebApiBaseUrl, interfaceName, methodName, methodVersion);

            bool isFirstParameter = true;
            string delimiter = String.Empty;
            foreach (var parameter in parameters)
            {
                if (isFirstParameter)
                {
                    delimiter = "?";
                    isFirstParameter = false;
                }
                else
                {
                    delimiter = "&";
                }

                string parameterString = String.Format("{0}{1}={2}", delimiter, parameter.Name, parameter.Value);
                command += parameterString;
            }

            return command;
        }
    }
}
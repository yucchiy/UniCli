using System;
using UnityEngine;

namespace UniCli.Remote.Commands
{
    [DebugCommand("Debug.GetPlayerPref", "Get a PlayerPrefs value by key")]
    public sealed class GetPlayerPrefCommand : DebugCommand<GetPlayerPrefCommand.Request, GetPlayerPrefCommand.Response>
    {
        protected override Response ExecuteCommand(Request request)
        {
            if (string.IsNullOrEmpty(request.key))
                throw new ArgumentException("'key' is required");

            if (!PlayerPrefs.HasKey(request.key))
            {
                return new Response
                {
                    key = request.key,
                    exists = false
                };
            }

            var stringValue = PlayerPrefs.GetString(request.key, null);
            var intValue = PlayerPrefs.GetInt(request.key, 0);
            var floatValue = PlayerPrefs.GetFloat(request.key, 0f);

            return new Response
            {
                key = request.key,
                exists = true,
                stringValue = stringValue,
                intValue = intValue,
                floatValue = floatValue
            };
        }

        [Serializable]
        public class Request
        {
            public string key;
        }

        [Serializable]
        public class Response
        {
            public string key;
            public bool exists;
            public string stringValue;
            public int intValue;
            public float floatValue;
        }
    }
}

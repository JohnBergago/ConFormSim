using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using Unity.MLAgents.SideChannels;

namespace ConFormSim.SideChannels
{
    public class IntListPropertiesChannel : SideChannel
    {
        private Dictionary<string, List<int>> m_IntListProperties = 
                                        new Dictionary<string, List<int>>();
        private Dictionary<string, Action<List<int>>> m_RegisteredActions = 
                                        new Dictionary<string, Action<List<int>>>();
        private const string k_IntListPropertiesDefaultId = "d554ba67-03ee-459e-974a-1cba23324c04";
        
        /// <summary>
        /// Initializes the side channel with the provided channel ID.
        /// </summary>
        /// <param name="channelId">ID for the side channel.</param>
        public IntListPropertiesChannel(Guid channelId = default(Guid))
        {
            if (channelId == default(Guid))
            {
                ChannelId = new Guid(k_IntListPropertiesDefaultId);
            }
            else
            {
                ChannelId = channelId;
            }
        }
        
        /// <inheritdoc/>
        protected override void OnMessageReceived(IncomingMessage msg)
        {
            var kv = DeserializeMessage(msg);
            m_IntListProperties[kv.Key] = kv.Value;
            if (m_RegisteredActions.ContainsKey(kv.Key))
            {
                m_RegisteredActions[kv.Key].Invoke(kv.Value);
            }
        }

        /// <summary>
        /// Sets one of the int list properties of the environment. This data will be
        /// sent to Python.
        /// </summary>
        /// <param name="key"> The string identifier of the property.</param>
        /// <param name="value"> The int list of the property.</param>
        public void SetProperty(string key, List<int> value)
        {
            m_IntListProperties[key] = value;
            QueueMessageToSend(SerializeMessage(key, value));

            Action<List<int>> action;
            m_RegisteredActions.TryGetValue(key, out action);
            action?.Invoke(value);
        }

        /// <summary>
        /// Get an Environment property with a default value. If there is a value for
        /// this property, it will be returned, otherwise, the default value will be
        /// returned.
        /// </summary>
        /// <param name="key"> The string identifier of the property.</param>
        /// <param name="defaultValue"> The default value of the property.</param>
        /// <returns></returns>
        public List<int> GetWithDefault(string key, List<int> defaultValue)
        {
            List<int> valueOut;
            bool hasKey = m_IntListProperties.TryGetValue(key, out valueOut);
            return hasKey ? valueOut : defaultValue;
        }

        /// <summary>
        /// Registers an action to be performed everytime the property is changed.
        /// </summary>
        /// <param name="key"> The string identifier of the property.</param>
        /// <param name="action"> The action that will be performed. Takes a float as
        /// input.</param>
        public void RegisterCallback(string key, Action<List<int>> action)
        {
            m_RegisteredActions[key] = action;
        }

        /// <summary>
        /// Returns a list of all the string identifiers of the properties currently
        /// present.
        /// </summary>
        /// <returns> The list of string identifiers </returns>
        public IList<string> ListProperties()
        {
            return new List<string>(m_IntListProperties.Keys);
        }

        private static KeyValuePair<string, List<int>> DeserializeMessage(IncomingMessage msg)
        {
            using (var memStream = new MemoryStream(msg.GetRawBytes()))
            {
                using (var binaryReader = new BinaryReader(memStream))
                {
                    var keyLength = binaryReader.ReadInt32();
                    var key = Encoding.ASCII.GetString(binaryReader.ReadBytes(keyLength));
                    var valueLength = binaryReader.ReadInt32();
                    List<int> value = new List<int>();
                    for (int i = 0; i < valueLength; i++)
                    {
                        value.Add(binaryReader.ReadInt32());
                    }
                    return new KeyValuePair<string, List<int>>(key, value);
                }
            }
        }

        private static OutgoingMessage SerializeMessage(string key, List<int> value)
        {
            using (var memStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memStream))
                {
                    var stringEncoded = Encoding.ASCII.GetBytes(key);
                    binaryWriter.Write(stringEncoded.Length);
                    binaryWriter.Write(stringEncoded);
                    binaryWriter.Write(value.Count);
                    foreach(int v in value)
                    {
                        binaryWriter.Write(v);
                    }
                    OutgoingMessage msg = new OutgoingMessage();
                    msg.SetRawBytes(memStream.ToArray());
                    return msg;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NetworkServer
{
    /// <summary>
    /// A custom message object
    /// </summary>
    /// <typeparam name="T">The type it has.</typeparam>
    [Serializable]
    public class Message<T>
    {

        public T Content { get; set; }

        //public Message(string type, T content)
        public Message(T content)
        {
            //Type = type;
            Content = content;
        }

        private bool IsValidStructType()
        {
            Type contentType = typeof(T);
            List<Type> registeredStructs = MessageTypeRegistry.Instance.GetRegisteredMessageTypes();

            return registeredStructs.Contains(contentType);
        }

        public byte[] ToBytes()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                return stream.ToArray();
            }
        }

        public static Message<T> FromBytes(byte[] data)
        {
            Message<T> message;

            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    message = (Message<T>)formatter.Deserialize(stream);

                    if (!message.IsValidStructType())
                    {
                        throw new InvalidOperationException($"Unregistered struct type: {typeof(T)}");
                    }
                }
            }
            catch (Exception e)
            {

                message = null;

            }

            return message;
        }

    }

}

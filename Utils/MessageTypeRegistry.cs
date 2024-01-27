using System.Numerics;

namespace NetworkServer
{
    /// <summary>
    /// Register message types.
    /// </summary>
    public class MessageTypeRegistry
    {

        /// <summary>
        /// A list of registered message types
        /// </summary>
        private List<Type> registeredMessageTypes;

        /// <summary>
        /// Make it a singleton
        /// </summary>
        private static MessageTypeRegistry instance;

        /// <summary>
        /// Get the instance
        /// </summary>
        public static MessageTypeRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MessageTypeRegistry();
                }
                return instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// \todo Standard types?
        private MessageTypeRegistry()
        {
            registeredMessageTypes = new List<Type>();
            RegisterMessageType(typeof(Vector3));
        }

        /// <summary>
        /// Register a message type.
        /// </summary>
        /// <param name="structType"></param>
        /// <exception cref="ArgumentException"></exception>
        public void RegisterMessageType(Type structType)
        {
            if (structType.IsValueType && !structType.IsPrimitive)
            {
                registeredMessageTypes.Add(structType);
            }
            else
            {
                //throw new ArgumentException($"{structType.FullName} is not a valid message type.");
            }
        }

        /// <summary>
        /// Get a list of registered message types
        /// </summary>
        /// <returns></returns>
        public List<Type> GetRegisteredMessageTypes()
        {
            return registeredMessageTypes;
        }

    }

}

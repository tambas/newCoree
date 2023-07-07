using System.Collections.Generic;
using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using Giny.Core.IO.Interfaces;
using Giny.Protocol;
using Giny.Protocol.Enums;

namespace Giny.Protocol.Messages
{ 
    public class ExchangeObjectRemovedFromBagMessage : ExchangeObjectMessage  
    { 
        public new const ushort Id = 9767;
        public override ushort MessageId => Id;

        public int objectUID;

        public ExchangeObjectRemovedFromBagMessage()
        {
        }
        public ExchangeObjectRemovedFromBagMessage(int objectUID,bool remote)
        {
            this.objectUID = objectUID;
            this.remote = remote;
        }
        public override void Serialize(IDataWriter writer)
        {
            base.Serialize(writer);
            if (objectUID < 0)
            {
                throw new System.Exception("Forbidden value (" + objectUID + ") on element objectUID.");
            }

            writer.WriteVarInt((int)objectUID);
        }
        public override void Deserialize(IDataReader reader)
        {
            base.Deserialize(reader);
            objectUID = (int)reader.ReadVarUhInt();
            if (objectUID < 0)
            {
                throw new System.Exception("Forbidden value (" + objectUID + ") on element of ExchangeObjectRemovedFromBagMessage.objectUID.");
            }

        }


    }
}









using System.Collections.Generic;
using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using Giny.Core.IO.Interfaces;
using Giny.Protocol;
using Giny.Protocol.Enums;

namespace Giny.Protocol.Messages
{ 
    public class HavenBagPermissionsUpdateRequestMessage : NetworkMessage  
    { 
        public  const ushort Id = 4471;
        public override ushort MessageId => Id;

        public int permissions;

        public HavenBagPermissionsUpdateRequestMessage()
        {
        }
        public HavenBagPermissionsUpdateRequestMessage(int permissions)
        {
            this.permissions = permissions;
        }
        public override void Serialize(IDataWriter writer)
        {
            if (permissions < 0)
            {
                throw new System.Exception("Forbidden value (" + permissions + ") on element permissions.");
            }

            writer.WriteInt((int)permissions);
        }
        public override void Deserialize(IDataReader reader)
        {
            permissions = (int)reader.ReadInt();
            if (permissions < 0)
            {
                throw new System.Exception("Forbidden value (" + permissions + ") on element of HavenBagPermissionsUpdateRequestMessage.permissions.");
            }

        }


    }
}









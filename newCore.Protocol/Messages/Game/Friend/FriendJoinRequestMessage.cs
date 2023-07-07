using System.Collections.Generic;
using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using Giny.Core.IO.Interfaces;
using Giny.Protocol;
using Giny.Protocol.Enums;

namespace Giny.Protocol.Messages
{ 
    public class FriendJoinRequestMessage : NetworkMessage  
    { 
        public  const ushort Id = 3127;
        public override ushort MessageId => Id;

        public AbstractPlayerSearchInformation target;

        public FriendJoinRequestMessage()
        {
        }
        public FriendJoinRequestMessage(AbstractPlayerSearchInformation target)
        {
            this.target = target;
        }
        public override void Serialize(IDataWriter writer)
        {
            writer.WriteShort((short)target.TypeId);
            target.Serialize(writer);
        }
        public override void Deserialize(IDataReader reader)
        {
            uint _id1 = (uint)reader.ReadUShort();
            target = ProtocolTypeManager.GetInstance<AbstractPlayerSearchInformation>((short)_id1);
            target.Deserialize(reader);
        }


    }
}









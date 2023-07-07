using System.Collections.Generic;
using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using Giny.Core.IO.Interfaces;
using Giny.Protocol;
using Giny.Protocol.Enums;

namespace Giny.Protocol.Messages
{ 
    public class TreasureHuntFlagRequestMessage : NetworkMessage  
    { 
        public  const ushort Id = 8059;
        public override ushort MessageId => Id;

        public byte questType;
        public byte index;

        public TreasureHuntFlagRequestMessage()
        {
        }
        public TreasureHuntFlagRequestMessage(byte questType,byte index)
        {
            this.questType = questType;
            this.index = index;
        }
        public override void Serialize(IDataWriter writer)
        {
            writer.WriteByte((byte)questType);
            if (index < 0)
            {
                throw new System.Exception("Forbidden value (" + index + ") on element index.");
            }

            writer.WriteByte((byte)index);
        }
        public override void Deserialize(IDataReader reader)
        {
            questType = (byte)reader.ReadByte();
            if (questType < 0)
            {
                throw new System.Exception("Forbidden value (" + questType + ") on element of TreasureHuntFlagRequestMessage.questType.");
            }

            index = (byte)reader.ReadByte();
            if (index < 0)
            {
                throw new System.Exception("Forbidden value (" + index + ") on element of TreasureHuntFlagRequestMessage.index.");
            }

        }


    }
}









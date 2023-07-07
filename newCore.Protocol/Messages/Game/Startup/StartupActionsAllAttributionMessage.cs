using System.Collections.Generic;
using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using Giny.Core.IO.Interfaces;
using Giny.Protocol;
using Giny.Protocol.Enums;

namespace Giny.Protocol.Messages
{ 
    public class StartupActionsAllAttributionMessage : NetworkMessage  
    { 
        public  const ushort Id = 789;
        public override ushort MessageId => Id;

        public long characterId;

        public StartupActionsAllAttributionMessage()
        {
        }
        public StartupActionsAllAttributionMessage(long characterId)
        {
            this.characterId = characterId;
        }
        public override void Serialize(IDataWriter writer)
        {
            if (characterId < 0 || characterId > 9.00719925474099E+15)
            {
                throw new System.Exception("Forbidden value (" + characterId + ") on element characterId.");
            }

            writer.WriteVarLong((long)characterId);
        }
        public override void Deserialize(IDataReader reader)
        {
            characterId = (long)reader.ReadVarUhLong();
            if (characterId < 0 || characterId > 9.00719925474099E+15)
            {
                throw new System.Exception("Forbidden value (" + characterId + ") on element of StartupActionsAllAttributionMessage.characterId.");
            }

        }


    }
}









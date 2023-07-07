using System.Collections.Generic;
using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using Giny.Core.IO.Interfaces;
using Giny.Protocol;
using Giny.Protocol.Enums;

namespace Giny.Protocol.Messages
{ 
    public class PartyDeletedMessage : AbstractPartyMessage  
    { 
        public new const ushort Id = 1536;
        public override ushort MessageId => Id;


        public PartyDeletedMessage()
        {
        }
        public PartyDeletedMessage(int partyId)
        {
            this.partyId = partyId;
        }
        public override void Serialize(IDataWriter writer)
        {
            base.Serialize(writer);
        }
        public override void Deserialize(IDataReader reader)
        {
            base.Deserialize(reader);
        }


    }
}









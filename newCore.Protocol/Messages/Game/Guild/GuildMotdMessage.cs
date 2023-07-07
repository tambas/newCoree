using System.Collections.Generic;
using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using Giny.Core.IO.Interfaces;
using Giny.Protocol;
using Giny.Protocol.Enums;

namespace Giny.Protocol.Messages
{ 
    public class GuildMotdMessage : SocialNoticeMessage  
    { 
        public new const ushort Id = 5546;
        public override ushort MessageId => Id;


        public GuildMotdMessage()
        {
        }
        public GuildMotdMessage(string content,int timestamp,long memberId,string memberName)
        {
            this.content = content;
            this.timestamp = timestamp;
            this.memberId = memberId;
            this.memberName = memberName;
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









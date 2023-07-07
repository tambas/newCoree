using System.Collections.Generic;
using Giny.Core.Network.Messages;
using Giny.Protocol.Types;
using Giny.Core.IO.Interfaces;
using Giny.Protocol;
using Giny.Protocol.Enums;

namespace Giny.Protocol.Messages
{ 
    public class ShortcutBarReplacedMessage : NetworkMessage  
    { 
        public  const ushort Id = 6652;
        public override ushort MessageId => Id;

        public byte barType;
        public Shortcut shortcut;

        public ShortcutBarReplacedMessage()
        {
        }
        public ShortcutBarReplacedMessage(byte barType,Shortcut shortcut)
        {
            this.barType = barType;
            this.shortcut = shortcut;
        }
        public override void Serialize(IDataWriter writer)
        {
            writer.WriteByte((byte)barType);
            writer.WriteShort((short)shortcut.TypeId);
            shortcut.Serialize(writer);
        }
        public override void Deserialize(IDataReader reader)
        {
            barType = (byte)reader.ReadByte();
            if (barType < 0)
            {
                throw new System.Exception("Forbidden value (" + barType + ") on element of ShortcutBarReplacedMessage.barType.");
            }

            uint _id2 = (uint)reader.ReadUShort();
            shortcut = ProtocolTypeManager.GetInstance<Shortcut>((short)_id2);
            shortcut.Deserialize(reader);
        }


    }
}









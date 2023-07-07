﻿using Giny.Core.IO;
using Giny.Zaap.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giny.Zaap.Protocol
{
    public class UserInfoGet : ZaapMessage
    {
        public enum TFieldId
        {
            GAMESESSION = 1,
        }
        public string GameSession
        {
            get;
            set;
        }
        public UserInfoGet() 
        {
        }

        public override void Deserialize(TProtocol protocol, BigEndianReader reader)
        {
            while (true)
            {
                var field = protocol.ReadFieldBegin(reader);

                if (field.Type == TType.STOP)
                {
                    break;
                }
                switch ((TFieldId)field.Id)
                {
                    case TFieldId.GAMESESSION:
                        this.GameSession = reader.ReadUTF7BitLength();
                        break;
                    default:
                        break;
                }
            }
        }

        public override void Serialize(TProtocol protocol, BigEndianWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}

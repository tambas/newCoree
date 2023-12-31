﻿using Giny.Core.IO;
using Giny.Zaap.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giny.Zaap.Protocol
{
    public class AuthGetGameToken : ZaapMessage
    {
    

        public enum TFieldId
        {
            GAMESESSION = 1,
            GAMEID = 2,
        }

        public string GameName
        {
            get;
            private set;
        }
        public int GameId
        {
            get;
            private set;
        }
        public AuthGetGameToken()  
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
                        this.GameName = reader.ReadUTF7BitLength();
                        break;
                    case TFieldId.GAMEID:
                        this.GameId = reader.ReadInt();
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

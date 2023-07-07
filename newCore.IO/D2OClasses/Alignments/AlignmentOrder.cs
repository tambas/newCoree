using System;
using Giny.Core.IO.Interfaces;
using Giny.IO.D2O;
using Giny.IO.D2OTypes;
using System.Collections.Generic;

namespace Giny.IO.D2OClasses
{    [D2OClass("AlignmentOrder", "")]
    public class AlignmentOrder : IDataObject , IIndexedData
    {        public const string MODULE = "AlignmentOrder";

        public int Id => (int)id;

        public int id;
        public uint nameId;
        public uint sideId;

        [D2OIgnore]
        public int Id_
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }
        [D2OIgnore]
        public uint NameId
        {
            get
            {
                return nameId;
            }
            set
            {
                nameId = value;
            }
        }
        [D2OIgnore]
        public uint SideId
        {
            get
            {
                return sideId;
            }
            set
            {
                sideId = value;
            }
        }

    }}

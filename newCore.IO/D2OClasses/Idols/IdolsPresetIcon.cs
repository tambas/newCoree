using System;
using Giny.Core.IO.Interfaces;
using Giny.IO.D2O;
using Giny.IO.D2OTypes;
using System.Collections.Generic;

namespace Giny.IO.D2OClasses
{    [D2OClass("IdolsPresetIcon", "")]
    public class IdolsPresetIcon : IDataObject , IIndexedData
    {        public const string MODULE = "IdolsPresetIcons";

        public int Id => (int)id;

        public int id;
        public int order;

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
        public int Order
        {
            get
            {
                return order;
            }
            set
            {
                order = value;
            }
        }

    }}

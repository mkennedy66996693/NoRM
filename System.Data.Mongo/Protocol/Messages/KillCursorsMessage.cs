﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Data.Mongo.Protocol.Messages
{
    internal class KillCursorsMessage : Message
    {
        private long[] _killCursors;
        internal KillCursorsMessage(MongoContext context, String fullyQualifiedCollName, params long[] cursorsToKill)
            : base(context, fullyQualifiedCollName)
        {
            this._killCursors = cursorsToKill;
        }
    }
}

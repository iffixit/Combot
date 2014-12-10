﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Combot
{
    internal partial class IRCService
    {
        public event Action<Message> MessageReceivedEvent;

        internal void ParseTCPMessage(string message)
        {
            Message parsedMessage = new Message();

            if (MessageReceivedEvent != null)
            {
                MessageReceivedEvent(parsedMessage);
            }
        }
    }
}

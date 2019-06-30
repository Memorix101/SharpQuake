using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    // Command execution takes a string, breaks it into tokens,
    // then searches for a command or variable that matches the first token.
    //
    // Commands can come from three sources, but the handler functions may choose
    // to dissallow the action or forward it to a remote server if the source is
    // not apropriate.

    public enum CommandSource
    {
        src_client,     // came in over a net connection as a clc_stringcmd

        // host_client will be valid during this state.
        src_command		// from the command buffer
    } // cmd_source_t
}

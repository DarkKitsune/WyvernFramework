using System.Collections.Generic;
using VulkanCore;

namespace WyvernFramework.Commands
{
    public static class CommandExtensions
    {
        /// <summary>
        /// Record a series of commands into a command buffer; buffer must currently be recording
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="buffer"></param>
        public static void RecordTo(this IEnumerable<Command> commands, CommandBuffer buffer)
        {
            foreach (var command in commands)
                command.RecordTo(buffer);
        }
    }
}

using System;
using VulkanCore;
using System.Collections.Generic;
using System.Linq;

namespace WyvernFramework.Commands
{
    /// <summary>
    /// Struct representing a Vulkan command
    /// </summary>
    public class Command
    {

        /// <summary>
        /// Pipeline stage this command will take place in
        /// </summary>
        public PipelineStages Stage { get; }

        /// <summary>
        /// Required image and its layout, or null
        /// </summary>
        public CommandImageLayout RequiredImageLayout { get; }

        /// <summary>
        /// The previous command, or null
        /// </summary>
        public Command Previous { get; private set; }

        /// <summary>
        /// Get if there is a previous command
        /// </summary>
        public bool HasPrevious => !(Previous is null);

        /// <summary>
        /// The next command, or null
        /// </summary>
        public Command Next { get; private set; }

        /// <summary>
        /// Get if there is a next command
        /// </summary>
        public bool HasNext => !(Next is null);

        /// <summary>
        /// The previous command using the same image memory
        /// </summary>
        public Command PreviousCommandUsingImageMemory
        {
            get
            {
                var prev = Previous;
                while (prev != null)
                {
                    if (prev.RequiredImageLayout != null)
                    {
                        var layout = prev.RequiredImageLayout;
                        if (layout.Image == RequiredImageLayout.Image && layout.Range.AspectMask == RequiredImageLayout.Range.AspectMask)
                            return prev;
                    }
                    prev = prev.Previous;
                }
                return null;
            }
        }

        /// <summary>
        /// The previous layout of this command's image
        /// </summary>
        public CommandImageLayout PreviousImageLayout
        {
            get
            {
                var prev = PreviousCommandUsingImageMemory;
                if (prev is null)
                    return null;
                return PreviousCommandUsingImageMemory.RequiredImageLayout;
            }
        }

        public Command(
                PipelineStages stage,
                CommandImageLayout requiredImageLayout = default,
                Command previous = default
            )
        {
            Previous = previous;
            if (!(Previous is null))
                Previous.Next = this;
            Stage = stage;
            RequiredImageLayout = requiredImageLayout;
        }
        
        /// <summary>
        /// Record this command into a command buffer
        /// </summary>
        /// <param name="buffer"></param>
        public virtual void RecordTo(CommandBuffer buffer)
        {
        }

        /// <summary>
        /// Create an image memory barrier for this command's image, returns null if not necessary
        /// </summary>
        /// <returns></returns>
        protected ImageMemoryBarrier GenerateImageMemoryBarrier()
        {
            // Check if there is an image and layout for this command
            if (RequiredImageLayout is null)
                throw new InvalidOperationException("Command does not have an image associated to it");
            // Get the previous layout if it exists
            var previousImageLayout = PreviousImageLayout;
            // Image was not in a known layout previously
            if (previousImageLayout is null)
            {
                // Return barrier from unknown layout
                return new ImageMemoryBarrier(
                        image: RequiredImageLayout.Image,
                        subresourceRange: RequiredImageLayout.Range,
                        srcAccessMask: Accesses.None,
                        dstAccessMask: RequiredImageLayout.Access,
                        oldLayout: ImageLayout.Undefined,
                        newLayout: RequiredImageLayout.Layout
                    );
            }
            // Image was in a known layout so make a barrier to transition it from that layout
            return new ImageMemoryBarrier(
                    image: RequiredImageLayout.Image,
                    subresourceRange: RequiredImageLayout.Range,
                    srcAccessMask: PreviousImageLayout.Access,
                    dstAccessMask: RequiredImageLayout.Access,
                    oldLayout: PreviousImageLayout.Layout,
                    newLayout: RequiredImageLayout.Layout
                );
        }

        /// <summary>
        /// Get the source stage for an image memory barrier between the previous commands and this command
        /// </summary>
        /// <returns></returns>
        protected PipelineStages GenerateImageMemoryBarrierSrcStageMask()
        {
            var prev = PreviousCommandUsingImageMemory;
            if (prev is null)
                return Stage;
            return prev.Stage;
        }

        public static IEnumerable<Command> operator +(Command a, Command b)
        {
            return new[] { a } + b;
        }

        public static IEnumerable<Command> operator +(IEnumerable<Command> a, Command b)
        {
            var last = a.Last();
            last.Next = b;
            b.Previous = last;
            return a.Append(b);
        }

        public static IEnumerable<Command> operator +(Command a, IEnumerable<Command> b)
        {
            var first = b.First();
            a.Next = first;
            first.Previous = a;
            return b.Prepend(a);
        }
    }
}

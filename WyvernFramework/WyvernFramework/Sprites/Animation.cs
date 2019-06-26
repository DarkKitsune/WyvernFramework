using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Collections.Generic;
using VulkanCore;

namespace WyvernFramework.Sprites
{
    public class Animation
    {
        public const int MaxInstructions = 64;
        internal static int SizeStd140 => (16 + Interop.SizeOf<ComputeInstruction>() * MaxInstructions).AlignSTD140(16);

        /// <summary>
        /// An animation instruction type
        /// </summary>
        public enum InstructionType
        {
            None,
            SetTime,
            SetScale,
            LerpScale
        }

        /// <summary>
        /// An animation instruction for use in a compute shader
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        internal struct ComputeInstruction
        {
            [FieldOffset(0)]
            public Vector4 Argument;
            [FieldOffset(16)]
            public int Type;
            [FieldOffset(20)]
            public float Time;

            public override string ToString()
            {
                return $"[ComputeInstruction Argument={Argument} Type={Type} Time={Time}]";
            }
        }

        /// <summary>
        /// An animation instruction
        /// </summary>
        public struct Instruction
        {
            public float Time;
            public InstructionType Type;
            public Vector4 ArgVec;

            internal ComputeInstruction ComputeInstruction => new ComputeInstruction
            {
                Time = Time,
                Type = (int)Type,
                Argument = ArgVec
            };

            public Instruction(float time, InstructionType type, Vector4 arg)
            {
                Time = time;
                Type = type;
                ArgVec = arg;
            }

            public Instruction(float time, InstructionType type, float arg)
            {
                Time = time;
                Type = type;
                ArgVec = new Vector4(arg, 0f, 0f, 0f);
            }

            public Instruction(float time, InstructionType type, Vector2 arg)
            {
                Time = time;
                Type = type;
                ArgVec = new Vector4(arg, 0f, 0f);
            }

            public Instruction(float time, InstructionType type, Vector3 arg)
            {
                Time = time;
                Type = type;
                ArgVec = new Vector4(arg, 0f);
            }

            public static Instruction SetTime(float time, float newTime)
            {
                return new Instruction(time, InstructionType.SetTime, newTime);
            }

            public static Instruction SetScale(float time, Vector2 scale)
            {
                return new Instruction(time, InstructionType.SetScale, scale);
            }

            public static Instruction LerpScale(float time, float length, Vector2 scale)
            {
                return new Instruction(time, InstructionType.LerpScale, new Vector3(length, scale.X, scale.Y));
            }
        }

        public Instruction[] Instructions { get; }

        public Animation(IEnumerable<Instruction> instructions)
        {
            if (instructions is null)
                throw new ArgumentNullException(nameof(instructions));
            Instructions = instructions.ToArray();
        }

        /// <summary>
        /// Write the animation in std140 format to a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="next"></param>
        internal unsafe void WriteToBuffer(byte* buffer, out byte* next)
        {
            var instructionSize = Interop.SizeOf<ComputeInstruction>();
            Debug.WriteLine($"Offset 0 = {Instructions.Length}");
            ((int*)buffer)[0] = Instructions.Length;
            buffer += 16;
            for (var i = 0; i < Instructions.Length; i++)
            {
                Debug.WriteLine($"Offset {16 + i * instructionSize} = {Instructions[i].ComputeInstruction}");
                *(ComputeInstruction*)(buffer + i * instructionSize) = Instructions[i].ComputeInstruction;
            }
            next = buffer - 16 + SizeStd140;
        }

        /// <summary>
        /// Write a null animation in std140 format to a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="next"></param>
        internal static unsafe void WriteNullToBuffer(byte* buffer, out byte* next)
        {
            *(int*)buffer = 0;
            next = buffer + SizeStd140;
        }
    }
}

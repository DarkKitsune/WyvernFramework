﻿using System;
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
            SetScale,
            LerpScale,
            SetRotation,
            LerpRotation,
            SetRectangle
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

            public Instruction(float time, InstructionType type)
            {
                Time = time;
                Type = type;
                ArgVec = new Vector4(0f, 0f, 0f, 0f);
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

            public static Instruction None(float time)
            {
                return new Instruction(time, InstructionType.None);
            }

            public static Instruction SetScale(float time, Vector2 scale)
            {
                return new Instruction(time, InstructionType.SetScale, scale);
            }

            public static Instruction LerpScale(float time, float length, Vector2 scale)
            {
                return new Instruction(time, InstructionType.LerpScale, new Vector3(length, scale.X, scale.Y));
            }

            public static Instruction SetRotation(float time, float rotation)
            {
                return new Instruction(time, InstructionType.SetRotation, rotation);
            }

            public static Instruction LerpRotation(float time, float length, float rotation)
            {
                return new Instruction(time, InstructionType.LerpRotation, new Vector2(length, rotation));
            }

            public static Instruction SetRectangle(float time, Vector2 topLeft, Vector2 size)
            {
                return new Instruction(time, InstructionType.SetRectangle, new Vector4(topLeft, size.X, size.Y));
            }
        }

        public Instruction[] Instructions { get; }

        public double LastTime => Instructions.Length == 0 ? 0.0 : Instructions[Instructions.Length - 1].Time;

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
            ((int*)buffer)[0] = Instructions.Length;
            buffer += 16;
            for (var i = 0; i < Instructions.Length; i++)
            {
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

        public Vector2 GetScale(float time, Vector2 baseScale = default)
        {
            time %= (float)LastTime;
            var scale = baseScale;
            for (var i = 0; i < Instructions.Length; i++)
            {
                var inst = Instructions[i];
                var applies = inst.Time <= time;
                var arg = inst.ArgVec;
                var interpLength = arg.X;
                var interpArg2 = new Vector2(arg.Y, arg.Z);
                float interpRatio = Math.Clamp((time - inst.Time) / interpLength, 0f, 1f);
                if (applies)
                {
                    switch (inst.Type)
                    {
                        case InstructionType.SetScale:
                            scale = new Vector2(arg.X, arg.Y);
                            break;
                        case InstructionType.LerpScale:
                            scale += (interpArg2 - scale) * interpRatio;
                            break;
                    }
                }
            }
            return scale;
        }

        public float GetRotation(float time, float baseRotation = default)
        {
            time %= (float)LastTime;
            var rotation = baseRotation;
            for (var i = 0; i < Instructions.Length; i++)
            {
                var inst = Instructions[i];
                var applies = inst.Time <= time;
                var arg = inst.ArgVec;
                var interpLength = arg.X;
                var interpArg = arg.Y;
                float interpRatio = Math.Clamp((time - inst.Time) / interpLength, 0f, 1f);
                if (applies)
                {
                    switch (inst.Type)
                    {
                        case InstructionType.SetRotation:
                            rotation = arg.X;
                            break;
                        case InstructionType.LerpRotation:
                            rotation += (interpArg - rotation) * interpRatio;
                            break;
                    }
                }
            }
            return rotation;
        }
    }
}

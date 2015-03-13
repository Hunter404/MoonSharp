﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;
using MoonSharp.Interpreter.Tree.Expressions;
using MoonSharp.Interpreter.Tree.Statements;

namespace MoonSharp.Interpreter.Tree.Fast_Interface
{
	internal static class Loader_Fast
	{
		internal static DynamicExprExpression LoadDynamicExpr(Script script, SourceCode source)
		{
			ScriptLoadingContext lcontext = CreateLoadingContext(script, source);
			lcontext.IsDynamicExpression = true;
			lcontext.Anonymous = true;

			Expression exp;
			using (script.PerformanceStats.StartStopwatch(Diagnostics.PerformanceCounter.AstCreation))
				exp = Expression.Expr(lcontext);

			return new DynamicExprExpression(exp, lcontext);
		}

		private static ScriptLoadingContext CreateLoadingContext(Script script, SourceCode source)
		{
			return new ScriptLoadingContext(script)
			{
				Scope = new BuildTimeScope(),
				Source = source,
				Lexer = new Lexer(source.Code, true)
			};
		}

		internal static int LoadChunk(Script script, SourceCode source, ByteCode bytecode, Table globalContext)
		{
			ScriptLoadingContext lcontext = CreateLoadingContext(script, source);

			Statement stat;

			using (script.PerformanceStats.StartStopwatch(Diagnostics.PerformanceCounter.AstCreation))
				stat = new ChunkStatement(lcontext, globalContext);

			int beginIp = -1;

			//var srcref = new SourceRef(source.SourceID);

			using (script.PerformanceStats.StartStopwatch(Diagnostics.PerformanceCounter.Compilation))
			using (bytecode.EnterSource(null))
			{
				bytecode.Emit_Nop(string.Format("Begin chunk {0}", source.Name));
				beginIp = bytecode.GetJumpPointForLastInstruction();
				stat.Compile(bytecode);
				bytecode.Emit_Nop(string.Format("End chunk {0}", source.Name));
			}

			//Debug_DumpByteCode(bytecode, source.SourceID);

			return beginIp;
		}

		internal static int LoadFunction(Script script, SourceCode source, ByteCode bytecode, Table globalContext)
		{
			ScriptLoadingContext lcontext = CreateLoadingContext(script, source);

			FunctionDefinitionExpression fnx;

			using (script.PerformanceStats.StartStopwatch(Diagnostics.PerformanceCounter.AstCreation))
				fnx = new FunctionDefinitionExpression(lcontext, globalContext);

			int beginIp = -1;

			//var srcref = new SourceRef(source.SourceID);

			using (script.PerformanceStats.StartStopwatch(Diagnostics.PerformanceCounter.Compilation))
			using (bytecode.EnterSource(null))
			{
				bytecode.Emit_Nop(string.Format("Begin function {0}", source.Name));
				beginIp = fnx.CompileBody(bytecode, source.Name);
				bytecode.Emit_Nop(string.Format("End function {0}", source.Name));
			}

			//Debug_DumpByteCode(bytecode, source.SourceID);

			return beginIp;

		}

	}
}

﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GameFramework;
namespace StorySystem
{
    public static class CustomCommandValueParser
    {
        public static Dsl.DslFile LoadStory(string file)
        {
            if (!string.IsNullOrEmpty(file)) {
                Dsl.DslFile dataFile = new Dsl.DslFile();
                var bytes = new byte[Dsl.DslFile.c_BinaryIdentity.Length];
                using (var fs = File.OpenRead(file)) {
                    fs.Read(bytes, 0, bytes.Length);
                    fs.Close();
                }
                var id = System.Text.Encoding.ASCII.GetString(bytes);
                if (id == Dsl.DslFile.c_BinaryIdentity) {
                    try {
                        dataFile.LoadBinaryFile(file);
                        return dataFile;
                    }
                    catch (Exception ex) {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendFormat("[LoadStory] LoadStory file:{0} Exception:{1}\n{2}", file, ex.Message, ex.StackTrace);
                        sb.AppendLine();
                        Helper.LogInnerException(ex, sb);
                        LogSystem.Error("{0}", sb.ToString());
                    }
                } else {
                    try {
                        if (dataFile.Load(file, LogSystem.Log)) {
                            return dataFile;
                        } else {
                            LogSystem.Error("LoadStory file:{0} failed", file);
                        }
                    } catch (Exception ex) {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendFormat("[LoadStory] LoadStory file:{0} Exception:{1}\n{2}", file, ex.Message, ex.StackTrace);
                        sb.AppendLine();
                        Helper.LogInnerException(ex, sb);
                        LogSystem.Error("{0}", sb.ToString());
                    }
                }
            }
            return null;
        }
        public static Dsl.DslFile LoadStoryText(string file, byte[] bytes)
        {
            if (Dsl.DslFile.IsBinaryDsl(bytes, 0)) {
                try {
                    Dsl.DslFile dataFile = new Dsl.DslFile();
                    dataFile.LoadBinaryCode(bytes);
                    return dataFile;
                }
                catch (Exception ex) {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendFormat("[LoadStory] LoadStoryText file:{0} Exception:{1}\n{2}", file, ex.Message, ex.StackTrace);
                    sb.AppendLine();
                    Helper.LogInnerException(ex, sb);
                    LogSystem.Error("{0}", sb.ToString());
                }
            } else {
                string text = Converter.FileContent2Utf8String(bytes);
                try {
                    Dsl.DslFile dataFile = new Dsl.DslFile();
                    if (dataFile.LoadFromString(text, file, LogSystem.Log)) {
                        return dataFile;
                    } else {
                        LogSystem.Error("LoadStoryText file:{0} failed", file);
                    }
                } catch (Exception ex) {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendFormat("[LoadStory] LoadStoryText file:{0} Exception:{1}\n{2}", file, ex.Message, ex.StackTrace);
                    sb.AppendLine();
                    Helper.LogInnerException(ex, sb);
                    LogSystem.Error("{0}", sb.ToString());
                }
            }
            return null;
        }
        public static void FirstParse(params Dsl.DslFile[] dataFiles)
        {
            for (int ix = 0; ix < dataFiles.Length; ++ix) {
                Dsl.DslFile dataFile = dataFiles[ix];
                FirstParse(dataFile.DslInfos);
            }
        }
        public static void FinalParse(params Dsl.DslFile[] dataFiles)
        {
            for (int ix = 0; ix < dataFiles.Length; ++ix) {
                Dsl.DslFile dataFile = dataFiles[ix];
                FinalParse(dataFile.DslInfos);
            }
        }
        public static void FirstParse(IList<Dsl.ISyntaxComponent> dslInfos)
        {
            for (int i = 0; i < dslInfos.Count; i++) {
                Dsl.ISyntaxComponent dslInfo = dslInfos[i];
                FirstParse(dslInfo);
            }
        }
        public static void FinalParse(IList<Dsl.ISyntaxComponent> dslInfos)
        {
            for (int i = 0; i < dslInfos.Count; i++) {
                Dsl.ISyntaxComponent dslInfo = dslInfos[i];
                FinalParse(dslInfo);
            }
        }
        public static void FirstParse(Dsl.ISyntaxComponent dslInfo)
        {            
            string id = dslInfo.GetId();
            if (id == "command") {
                StorySystem.CommonCommands.CompositeCommand cmd = new CommonCommands.CompositeCommand();
                cmd.InitSharedData();
                var first = dslInfo as Dsl.FunctionData;
                if (null != first) {
                    cmd.Name = first.Call.GetParamId(0);
                }
                else {
                    var statement = dslInfo as Dsl.StatementData;
                    if (null != statement) {
                        first = statement.First;
                        cmd.Name = first.Call.GetParamId(0);
                        for (int i = 1; i < statement.GetFunctionNum(); ++i) {
                            var funcData = statement.GetFunction(i);
                            var fid = funcData.GetId();
                            if (fid == "args") {
                                for (int ix = 0; ix < funcData.Call.GetParamNum(); ++ix) {
                                    cmd.ArgNames.Add(funcData.Call.GetParamId(ix));
                                }
                            }
                            else if (fid == "opts") {
                                for (int ix = 0; ix < funcData.GetStatementNum(); ++ix) {
                                    var fcomp = funcData.GetStatement(ix);
                                    var fcd = fcomp as Dsl.CallData;
                                    if (null != fcd) {
                                        cmd.OptArgs.Add(fcd.GetId(), fcd.GetParam(0));
                                    }
                                }
                            }
                            else if (fid == "body") {
                            }
                            else {
                                LogSystem.Error("Command {0} unknown part '{1}'", cmd.Name, fid);
                            }
                        }
                    }
                }
                //注册
                StoryCommandManager.Instance.RegisterCommandFactory(cmd.Name, new CommonCommands.CompositeCommandFactory(cmd), true);
            } else if (id == "value") {
                StorySystem.CommonValues.CompositeValue val = new CommonValues.CompositeValue();
                val.InitSharedData();
                var first = dslInfo as Dsl.FunctionData;
                if (null != first) {
                    val.Name = first.Call.GetParamId(0);
                }
                else {
                    var statement = dslInfo as Dsl.StatementData;
                    if (null != statement) {
                        first = statement.First;
                        val.Name = first.Call.GetParamId(0);
                        for (int i = 1; i < statement.GetFunctionNum(); ++i) {
                            var funcData = statement.GetFunction(i);
                            var fid = funcData.GetId();
                            if (fid == "args") {
                                for (int ix = 0; ix < funcData.Call.GetParamNum(); ++ix) {
                                    val.ArgNames.Add(funcData.Call.GetParamId(ix));
                                }
                            }
                            else if (fid == "ret") {
                                val.ReturnName = funcData.Call.GetParamId(0);
                            }
                            else if (fid == "opts") {
                                for (int ix = 0; ix < funcData.GetStatementNum(); ++ix) {
                                    var fcomp = funcData.GetStatement(ix);
                                    var fcd = fcomp as Dsl.CallData;
                                    if (null != fcd) {
                                        val.OptArgs.Add(fcd.GetId(), fcd.GetParam(0));
                                    }
                                }
                            }
                            else if (fid == "body") {
                            }
                            else {
                                LogSystem.Error("Value {0} unknown part '{1}'", val.Name, fid);
                            }
                        }
                    }
                }
                //注册
                StoryValueManager.Instance.RegisterValueFactory(val.Name, new CommonValues.CompositeValueFactory(val), true);
            }
        }
        public static void FinalParse(Dsl.ISyntaxComponent dslInfo)
        {
            string id = dslInfo.GetId();
            if (id == "command") {
                string name = string.Empty;
                var first = dslInfo as Dsl.FunctionData;
                var statement = dslInfo as Dsl.StatementData;
                if (null != first) {
                    name = first.Call.GetParamId(0);
                }
                else {
                    if (null != statement) {
                        first = statement.First;
                        name = first.Call.GetParamId(0);
                    }
                }

                IStoryCommandFactory factory = StoryCommandManager.Instance.FindFactory(name);
                if (null != factory) {
                    StorySystem.CommonCommands.CompositeCommand cmd = factory.Create() as StorySystem.CommonCommands.CompositeCommand;
                    cmd.InitialCommands.Clear();

                    Dsl.FunctionData bodyFunc = null;
                    if (null != statement) {
                        for (int i = 0; i < statement.GetFunctionNum(); ++i) {
                            var funcData = statement.GetFunction(i);
                            var fid = funcData.GetId();
                            if (funcData.HaveStatement() && fid != "opts") {
                                bodyFunc = funcData;
                            }
                        }
                    }
                    else {
                        bodyFunc = first;
                    }
                    if (null != bodyFunc) {
                        for (int ix = 0; ix < bodyFunc.GetStatementNum(); ++ix) {
                            Dsl.ISyntaxComponent syntaxComp = bodyFunc.GetStatement(ix);
                            IStoryCommand sub = StoryCommandManager.Instance.CreateCommand(syntaxComp);
                            cmd.InitialCommands.Add(sub);
                        }
                    }
                    else {
                        LogSystem.Error("Can't find command {0}'s body", name);
                    }
                } else {
                    LogSystem.Error("Can't find command {0}'s factory", name);
                }
            } else if (id == "value") {
                string name = string.Empty;
                var first = dslInfo as Dsl.FunctionData;
                var statement = dslInfo as Dsl.StatementData;
                if (null != first) {
                    name = first.Call.GetParamId(0);
                }
                else {
                    if (null != statement) {
                        first = statement.First;
                        name = first.Call.GetParamId(0);
                    }
                }

                IStoryValueFactory factory = StoryValueManager.Instance.FindFactory(name);
                if (null != factory) {
                    StorySystem.CommonValues.CompositeValue val = factory.Build() as StorySystem.CommonValues.CompositeValue;
                    val.InitialCommands.Clear();

                    Dsl.FunctionData bodyFunc = null;
                    if (null != statement) {
                        for (int i = 0; i < statement.GetFunctionNum(); ++i) {
                            var funcData = statement.GetFunction(i);
                            var fid = funcData.GetId();
                            if (funcData.HaveStatement() && fid != "opts") {
                                bodyFunc = funcData;
                            }
                        }
                    }
                    else {
                        bodyFunc = first;
                    }
                    if (null != bodyFunc) {
                        for (int ix = 0; ix < bodyFunc.GetStatementNum(); ++ix) {
                            Dsl.ISyntaxComponent syntaxComp = bodyFunc.GetStatement(ix);
                            IStoryCommand sub = StoryCommandManager.Instance.CreateCommand(syntaxComp);
                            val.InitialCommands.Add(sub);
                        }
                    }
                    else {
                        LogSystem.Error("Can't find value {0}'s body", name);
                    }
                } else {
                    LogSystem.Error("Can't find value {0}'s factory", name);
                }
            }
        }       
    }
}

﻿
using System;
using System.Collections.Generic;


namespace SLua
{
    public partial class LuaDelegation : LuaObject
    {

        static internal int checkDelegate(IntPtr l,int p,out ProtoBuf.Meta.TypeFormatEventHandler ua) {
            int op = extractFunction(l,p);
			if(LuaDLL.lua_isnil(l,p)) {
				ua=null;
				return op;
			}
            else if (LuaDLL.lua_isuserdata(l, p)==1)
            {
                ua = (ProtoBuf.Meta.TypeFormatEventHandler)checkObj(l, p);
                return op;
            }
            LuaDelegate ld;
            checkType(l, -1, out ld);
            if(ld.d!=null)
            {
                ua = (ProtoBuf.Meta.TypeFormatEventHandler)ld.d;
                return op;
            }
			LuaDLL.lua_pop(l,1);
			
			l = LuaState.get(l).L;
            ua = (System.Object a1,ProtoBuf.Meta.TypeFormatEventArgs a2) =>
            {
                int error = pushTry(l);

				pushValue(l,a1);
				pushValue(l,a2);
				ld.pcall(2, error);
				LuaDLL.lua_settop(l, error-1);
			};
			ld.d=ua;
			return op;
		}
	}
}

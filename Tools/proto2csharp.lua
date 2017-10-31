--------------------------------------------------------------------------
-- protoתc#
-- author: rick.han
-- date  : 2015/03/06
--------------------------------------------------------------------------

-- 先更新此目录吧
os.execute("TortoiseProc.exe /command:update  /path:\".\"")

-- 替换掉字符串
function ReplaceString(str, ori_str, rep_str)
	if not str then
		return ""
	end
	
	local index = string.find(str, ori_str)
	if not index then
		return str
	end
	
	local ret_str = string.sub(str, 1, index-1)
	ret_str = ret_str .. rep_str
	ret_str = ret_str .. ReplaceString(string.sub(str, index+string.len(ori_str)), ori_str, rep_str)
	return ret_str
end

local output_file_list = 
{
	["CommonProto.cs"] = ".\\tools\\ProtoGen\\protogen.exe -i:.\\common.proto -o:CommonProto.cs",
	["RetCode.cs"] = ".\\tools\\ProtoGen\\protogen.exe -i:.\\ret_code.proto -o:RetCode.cs",
	["Login.cs"] = ".\\tools\\ProtoGen\\protogen.exe -i:.\\login.proto -o:Login.cs",
	["ChatProto.cs"] = ".\\tools\\ProtoGen\\protogen.exe -i:.\\chat.proto -o:ChatProto.cs",
	["MapDataProto.cs"] = ".\\tools\\ProtoGen\\protogen.exe -i:.\\mapdata.proto -o:MapDataProto.cs",
	["BattleProto.cs"] = ".\\tools\\ProtoGen\\protogen.exe -i:.\\battle.proto -o:BattleProto.cs",
}

for k,v in pairs(output_file_list) do
	os.execute(v)
end

-- 替换c#中ProtoBuf为MuffinProtoBuf
for k,v in pairs(output_file_list) do
	local str = ""
	for row in io.lines(k) do
		str = str .. ReplaceString(row, "ProtoBuf", "InfibrProtoBuf")
		str = str .. "\n"
	end
	
	local f = io.open(k, "w+")
	f:write(str)
	f:close()
end

local dir = "C:\\Dev\\infibr\\client\\trunk\\BattleRoyale\\Assets\\BattleRoyale\\Resources\\Others\\LogicScript\\Core\\Protocol"
os.execute("move .\\*.cs C:\\Dev\\infibr\\client\\trunk\\BattleRoyale\\Assets\\BattleRoyale\\Resources\\Others\\LogicScript\\Core\\Protocol")

-- 直接自动提交到svn
local svn = "TortoiseProc.exe /command:commit  /path:"
for k,v in pairs(output_file_list) do
	svn = svn .. dir .. "\\" .. k .. "*"
end
svn = svn .. " /logmsg:\"PROTOBUF TO CSHARP CODE GENERATED AND COMMITED AUTOMATIC, COMMIT TIME:" .. os.date() .. "\" /closeonend:0"
os.execute(svn)
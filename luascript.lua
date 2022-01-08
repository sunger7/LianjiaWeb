database = sqlitedatabase:new() 

database:filename("D:\\CodeProgram\\LianjiaWebWorm\\bin\\house.db") 

database:connected(true) 

query = sqlitequery:new() 

query:database(database) 
filter = "where date > '2020.08.01' and date < '2020.09.01'";
filter1 = "SELECT count(*) FROM "
query:sql(filter1..  "[main].普陀 " .. filter) 
query:active(true) 
print("普陀"..query:getfield(0):value().."\n"); 
print()
query:sql(filter1..  "[main].徐汇 " .. filter) 
query:active(true) 
print("徐汇"..query:getfield(0):value().."\n"); 
query:sql(filter1..  "   [main].青浦 " .. filter) 
query:active(true) 
print("青浦"..query:getfield(0):value().."\n"); 
query:sql(filter1..  " [main].长宁 " .. filter) 
query:active(true) 
print("长宁"..query:getfield(0):value().."\n"); 
query:free() 
database:free() 

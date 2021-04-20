### Excel

Ubiq logs structured events, which are amenable to being viewed in a table. Microsoft Excel PowerQuery can import Json files from `LogCollector`s and display events as a table.
To do this,

1. Open a new Workbook
2. Data -> Get Data -> From File -> From Json
3. Open the Log File
4. Select the List header and click Convert To Table. This will instruct Excel to treat each entry as a row.
5. Leave the Default Values in place and Click OK. The View will now appear as a Column
6. Use the button in the top Right to Add the Expand Column Step. This will split each record into a set of columns. Make sure to Click Load More if visible to ensure you get every possible field in the table.
7. Click OK
8. Click Close & Load to build your table.

You can now order by Ticks, and filter columns such as Events.


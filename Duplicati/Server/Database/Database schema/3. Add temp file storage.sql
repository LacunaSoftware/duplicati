/*
Long-term temporary file records
*/
CREATE TABLE "TempFile" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1, 1),
    "Origin" NVARCHAR(450) NOT NULL, 
    "Path" NVARCHAR(450) NOT NULL, 
    "Timestamp" INTEGER NOT NULL,
    "Expires" INTEGER NOT NULL
);


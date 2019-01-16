/*
Long-term temporary file records
*/
CREATE TABLE "TempFile" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1, 1),
    "Origin" TEXT NOT NULL, 
    "Path" TEXT NOT NULL, 
    "Timestamp" INTEGER NOT NULL,
    "Expires" INTEGER NOT NULL
);


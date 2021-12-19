
DROP FUNCTION generic_getblobs(idlist TEXT,
                                      start TIMESTAMP,
                                      "end" TIMESTAMP,
                                      ofst INTEGER ,
                                      lim INTEGER,
                                      direction VARCHAR(12));
CREATE OR REPLACE FUNCTION generic_getblobs(idlist TEXT,
                                      start TIMESTAMP WITH TIME ZONE,
                                      "end" TIMESTAMP WITH TIME ZONE,
                                      ofst INTEGER DEFAULT NULL,
                                      lim INTEGER DEFAULT NULL,
                                      direction VARCHAR(12) DEFAULT 'ASC'
                                      )
    RETURNS TABLE(
        "ID" BIGINT,
        "SensorID" VARCHAR(24),
        "Path" TEXT,
        "StorageType" INTEGER,
        "Timestamp" TIMESTAMP WITH TIME ZONE,
        "FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
    DECLARE sensorIds VARCHAR(24)[];
BEGIN
    sensorIds = ARRAY(SELECT DISTINCT UNNEST(string_to_array(idlist, ',')));

    IF upper(direction) NOT IN ('ASC', 'DESC', 'ASCENDING', 'DESCENDING') THEN
      RAISE EXCEPTION 'Unexpected value for parameter direction.
                       Allowed: ASC, DESC, ASCENDING, DESCENDING. Default: ASC';
   END IF;

	RETURN QUERY EXECUTE
	    format('SELECT "ID", "SensorID", "Path", "StorageType", "Timestamp", "FileSize" ' ||
	           'FROM "Blobs" ' ||
	           'WHERE "Timestamp" >= $1 AND "Timestamp" < $2 AND "SensorID" = ANY($3) ' ||
	           'ORDER BY "Timestamp" %s ' ||
	           'OFFSET %s ' ||
	           'LIMIT %s',
	        direction, ofst, lim)
    USING start, "end", sensorIds;
END
$$;

DROP FUNCTION networkapi_selectblobsbysensorid(sensorid VARCHAR(24), offst INTEGER, lim INTEGER);
CREATE OR REPLACE FUNCTION networkapi_selectblobsbysensorid(sensorid VARCHAR(24), offst INTEGER DEFAULT NULL, lim INTEGER DEFAULT NULL)
    RETURNS TABLE(
       	"ID" BIGINT,
		"SensorID" VARCHAR(24),
		"FileName" TEXT,
		"Path" TEXT,
		"StorageType" INTEGER,
		"Timestamp" TIMESTAMP WITH TIME ZONE,
		"FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	
	SELECT b."ID",
	       b."SensorID",
		   b."FileName",
		   b."Path",
		   b."StorageType",
		   b."Timestamp",
		   b."FileSize"
	FROM "Blobs" AS b
	WHERE b."SensorID" = sensorid
	OFFSET offst
	LIMIT lim;
END;
$$;

DROP FUNCTION networkapi_createblob(sensorid VARCHAR(24), filename TEXT, path TEXT, storage INTEGER, filesize INTEGER);
CREATE OR REPLACE FUNCTION networkapi_createblob(sensorid VARCHAR(24), filename TEXT, path TEXT, storage INTEGER, filesize INTEGER)
    RETURNS TABLE(
       	"ID" BIGINT,
		"SensorID" VARCHAR(24),
		"FileName" TEXT,
		"Path" TEXT,
		"StorageType" INTEGER,
		"Timestamp" TIMESTAMP WITH TIME ZONE,
		"FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    INSERT INTO "Blobs" ("SensorID",
                         "FileName",
                         "Path",
                         "StorageType",
                         "FileSize",
                         "Timestamp")
    VALUES (sensorid,
            filename,
            path,
            storage,
            filesize,
            NOW())
    RETURNING
       	"Blobs"."ID",
		"Blobs"."SensorID",
		"Blobs"."FileName",
		"Blobs"."Path",
		"Blobs"."StorageType",
		"Blobs"."Timestamp",
		"Blobs"."FileSize";
END;
$$;

DROP FUNCTION networkapi_deleteblobbyid(id BIGINT);
CREATE OR REPLACE FUNCTION networkapi_deleteblobbyid(id BIGINT)
    RETURNS TABLE(
       	"ID" BIGINT,
		"SensorID" VARCHAR(24),
		"FileName" TEXT,
		"Path" TEXT,
		"StorageType" INTEGER,
		"Timestamp" TIMESTAMP WITH TIME ZONE,
		"FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN

	RETURN QUERY	
	DELETE FROM "Blobs"
	WHERE "Blobs"."ID" = id
	RETURNING 
	       "Blobs"."ID",
	       "Blobs"."SensorID",
		   "Blobs"."FileName",
		   "Blobs"."Path",
		   "Blobs"."StorageType",
		   "Blobs"."Timestamp",
		   "Blobs"."FileSize";
END;
$$;

DROP FUNCTION networkapi_deleteblobsbysensorid(sensorid VARCHAR(24));
CREATE OR REPLACE FUNCTION networkapi_deleteblobsbysensorid(sensorid VARCHAR(24))
    RETURNS TABLE(
       	"ID" BIGINT,
		"SensorID" VARCHAR(24),
		"FileName" TEXT,
		"Path" TEXT,
		"StorageType" INTEGER,
		"Timestamp" TIMESTAMP WITH TIME ZONE,
		"FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN

	RETURN QUERY	
	DELETE FROM "Blobs"
	WHERE "Blobs"."SensorID" = sensorid
	RETURNING 
	       "Blobs"."ID",
	       "Blobs"."SensorID",
		   "Blobs"."FileName",
		   "Blobs"."Path",
		   "Blobs"."StorageType",
		   "Blobs"."Timestamp",
		   "Blobs"."FileSize";
END;
$$;

DROP FUNCTION networkapi_deleteblobsbyname(sensorid VARCHAR(24), filename TEXT);
CREATE OR REPLACE FUNCTION networkapi_deleteblobsbyname(sensorid VARCHAR(24), filename TEXT)
    RETURNS TABLE(
       	"ID" BIGINT,
		"SensorID" VARCHAR(24),
		"FileName" TEXT,
		"Path" TEXT,
		"StorageType" INTEGER,
		"Timestamp" TIMESTAMP WITH TIME ZONE,
		"FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN

	RETURN QUERY	
	DELETE FROM "Blobs"
	WHERE "Blobs"."SensorID" = sensorid AND
	      "Blobs"."FileName" = filename
	RETURNING 
	       "Blobs"."ID",
	       "Blobs"."SensorID",
		   "Blobs"."FileName",
		   "Blobs"."Path",
		   "Blobs"."StorageType",
		   "Blobs"."Timestamp",
		   "Blobs"."FileSize";
END;
$$;

DROP FUNCTION networkapi_selectblobbyid(id BIGINT);
CREATE OR REPLACE FUNCTION networkapi_selectblobbyid(id BIGINT)
    RETURNS TABLE(
       	"ID" BIGINT,
		"SensorID" VARCHAR(24),
		"FileName" TEXT,
		"Path" TEXT,
		"StorageType" INTEGER,
		"Timestamp" TIMESTAMP WITH TIME ZONE,
		"FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN

	RETURN QUERY	
	SELECT b."ID",
	       b."SensorID",
		   b."FileName",
		   b."Path",
		   b."StorageType",
		   b."Timestamp",
		   b."FileSize"
	FROM "Blobs" AS b
	WHERE b."ID" = id;
END;
$$;

DROP FUNCTION networkapi_selectblobbyname(sensorid VARCHAR(24), filename TEXT);
CREATE OR REPLACE FUNCTION networkapi_selectblobbyname(sensorid VARCHAR(24), filename TEXT)
    RETURNS TABLE(
       	"ID" BIGINT,
		"SensorID" VARCHAR(24),
		"FileName" TEXT,
		"Path" TEXT,
		"StorageType" INTEGER,
		"Timestamp" TIMESTAMP WITH TIME ZONE,
		"FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	
	SELECT b."ID",
	       b."SensorID",
		   b."FileName",
		   b."Path",
		   b."StorageType",
		   b."Timestamp",
		   b."FileSize"
	FROM "Blobs" AS b
	WHERE b."SensorID" = sensorid AND
	      b."FileName" = filename;
END;
$$;

DROP FUNCTION networkapi_selectblobs(idlist TEXT, offst INTEGER, lim INTEGER);
CREATE OR REPLACE FUNCTION networkapi_selectblobs(idlist TEXT, offst INTEGER DEFAULT NULL, lim INTEGER DEFAULT NULL)
    RETURNS TABLE(
       	"ID" BIGINT,
		"SensorID" VARCHAR(24),
		"FileName" TEXT,
		"Path" TEXT,
		"StorageType" INTEGER,
		"Timestamp" TIMESTAMP WITH TIME ZONE,
		"FileSize" BIGINT
                 )
    LANGUAGE plpgsql
AS $$
DECLARE sensorIds VARCHAR(24)[];
BEGIN
	sensorIds = ARRAY(SELECT DISTINCT UNNEST(string_to_array(idlist, ',')));

	RETURN QUERY	
	SELECT b."ID",
	       b."SensorID",
		   b."FileName",
		   b."Path",
		   b."StorageType",
		   b."Timestamp",
		   b."FileSize"
	FROM "Blobs" AS b
	WHERE b."SensorID" = ANY (sensorIds)
	OFFSET offst
	LIMIT lim;
END;
$$;

GRANT EXECUTE ON FUNCTION networkapi_selectblobs(text,integer,integer) TO db_networkapi;
GRANT EXECUTE ON FUNCTION networkapi_selectblobbyid(bigint) TO db_networkapi;
GRANT EXECUTE ON FUNCTION networkapi_selectblobbyname(character varying,text) TO db_networkapi;
GRANT EXECUTE ON FUNCTION networkapi_selectblobsbysensorid(character varying,integer,integer) TO db_networkapi;
GRANT EXECUTE ON FUNCTION networkapi_deleteblobbyid(bigint) TO db_networkapi;
GRANT EXECUTE ON FUNCTION networkapi_deleteblobsbyname(character varying,text) TO db_networkapi;
GRANT EXECUTE ON FUNCTION networkapi_deleteblobsbysensorid(character varying) TO db_networkapi;
GRANT EXECUTE ON FUNCTION generic_getblobs(TEXT, TIMESTAMP WITH TIME ZONE, TIMESTAMP WITH TIME ZONE, INTEGER, INTEGER, VARCHAR(3)) TO db_networkapi;
GRANT EXECUTE ON FUNCTION networkapi_createblob(character varying,text,text,integer,integer) TO db_networkapi;
GRANT EXECUTE ON FUNCTION generic_getblobs(TEXT, TIMESTAMP WITH TIME ZONE, TIMESTAMP WITH TIME ZONE, INTEGER, INTEGER, VARCHAR(3)) TO db_dataapi;

SET TimeZone='UTC';
ALTER TABLE "Blobs" ALTER COLUMN "Timestamp" TYPE TIMESTAMP WITH TIME ZONE;



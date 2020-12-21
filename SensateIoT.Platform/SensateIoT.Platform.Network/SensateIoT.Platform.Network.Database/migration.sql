CREATE FUNCTION livedataservice_getapikey(userid TEXT, apikey TEXT)
    RETURNS TABLE("UserId" TEXT, "Type" INT)
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	
	SELECT key."UserId", key."Type"
	FROM public."ApiKeys" AS key
	WHERE key."UserId" = userid AND
	      key."ApiKey" = apikey AND
	      key."Type"   != 0 AND
	      key."Revoked" = False;
END;
$$;
CREATE FUNCTION networkapi_createaction(
    triggerid BIGINT,
    channel INT,
    target VARCHAR(255),
    message TEXT
)
    RETURNS TABLE(
        "ID" BIGINT,
        "TriggerID" BIGINT,
        "Channel" INT,
        "Target" VARCHAR(255),
        "Message" TEXT
    )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    INSERT INTO "TriggerActions" ("TriggerID" ,
                                  "Channel" ,
                                  "Target" ,
                                  "Message")
    VALUES (triggerid, channel, target, message)
    RETURNING
        "TriggerActions"."ID",
        "TriggerActions"."TriggerID",
        "TriggerActions"."Channel",
        "TriggerActions"."Target",
        "TriggerActions"."Message";
END
$$
CREATE FUNCTION networkapi_createinvocation(
    triggerid BIGINT,
    actionid BIGINT,
    timestmp TIMESTAMP
)
    RETURNS TABLE(
        "ID" BIGINT,
        "TriggerID" BIGINT,
        "ActionID" BIGINT,
        "Timestamp" TIMESTAMP
    )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    INSERT INTO "TriggerInvocations" ("TriggerID",
                                      "ActionID",
                                      "Timestamp")
    VALUES (triggerid,
            actionid,
            timestmp)
    RETURNING
        "TriggerInvocations"."ID",
        "TriggerInvocations"."TriggerID",
        "TriggerInvocations"."ActionID",
        "TriggerInvocations"."Timestamp";
END
$$
CREATE FUNCTION networkapi_createsensorkey(key TEXT, userId TEXT)
    RETURNS TABLE(
        "Id" UUID,
        "UserId" UUID,
        "ApiKey" TEXT,
        "Revoked" BOOLEAN,
        "Type" INT,
        "ReadOnly" BOOLEAN
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    INSERT INTO "ApiKeys" (
                           "Id",
                           "UserId",
                           "ApiKey",
                           "Revoked",
                           "CreatedOn",
                           "Type"
                           )
    VALUES (
            uuid_generate_v4(),
            userId,
            key,
            FALSE,
            NOW(),
            0
    )
    RETURNING
        "ApiKeys"."Id"::UUID,
        "ApiKeys"."UserId"::UUID,
        "ApiKeys"."ApiKey",
        "ApiKeys"."Revoked",
        "ApiKeys"."Type",
        "ApiKeys"."ReadOnly";
END;
$$
CREATE FUNCTION networkapi_createsensorlink(sensorid VARCHAR(24), userid UUID)
    RETURNS TABLE(
        "SensorID" VARCHAR(24),
        "UserID" UUID
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
    INSERT INTO "SensorLinks" ("SensorId",
                              "UserId")
    VALUES (sensorid, userid::TEXT)
    RETURNING
        "SensorLinks"."SensorId"::VARCHAR(24),
        "SensorLinks"."UserId"::UUID;
END
$$
CREATE FUNCTION networkapi_createtrigger(
    sensorid VARCHAR(24),
    keyvalue VARCHAR(32),
    loweredge NUMERIC,
    upperedge NUMERIC,
    formallanguage TEXT,
    type INT
)
    RETURNS TABLE(
        "ID" BIGINT,
        "SensorID" VARCHAR(24),
        "KeyValue" VARCHAR(32),
        "LowerEdge" NUMERIC,
        "UpperEdge" NUMERIC,
        "FormalLanguage" TEXT,
        "Type" INT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    INSERT INTO "Triggers" ("SensorID",
                           "KeyValue",
                           "LowerEdge",
                           "UpperEdge",
                           "FormalLanguage",
                           "Type")
    VALUES (sensorid,
            keyvalue,
            loweredge,
            upperedge,
            formallanguage,
            type)
    RETURNING
        "Triggers"."ID",
        "Triggers"."SensorID",
        "Triggers"."KeyValue",
        "Triggers"."LowerEdge",
        "Triggers"."UpperEdge",
        "Triggers"."FormalLanguage",
        "Triggers"."Type";
END
$$

CREATE FUNCTION networkapi_deletesensorkey(key TEXT)
    RETURNS VOID
    LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM "ApiKeys"
    WHERE "ApiKey" = key;
END;
$$;
CREATE FUNCTION networkapi_deletesensorlink(sensorid VARCHAR(24), userid UUID)
    RETURNS TABLE(
        "SensorID" VARCHAR(24),
        "UserID" UUID
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	DELETE FROM "SensorLinks"
    WHERE "SensorLinks"."UserId" = userid::TEXT AND
          "SensorLinks"."SensorId" = sensorid::TEXT
    RETURNING
        "SensorLinks"."SensorId"::VARCHAR(24),
        "SensorLinks"."UserId"::UUID;
END
$$

CREATE FUNCTION networkapi_deletetriggeraction(triggerid BIGINT, channel INT)
    RETURNS TABLE(
        "ID" BIGINT,
        "TriggerID" BIGINT,
        "Channel" INT,
        "Target" VARCHAR(24),
        "Message" TEXT
    )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    DELETE FROM "TriggerActions"
    WHERE "TriggerActions"."TriggerID" = triggerid AND "TriggerActions"."Channel" = channel
	RETURNING
        "TriggerActions"."ID",
	    "TriggerActions"."TriggerID",
	    "TriggerActions"."Channel",
	    "TriggerActions"."Target",
	    "TriggerActions"."Message";
END
$$
CREATE FUNCTION networkapi_deletetriggerbyid(id BIGINT)
    RETURNS TABLE(
        "ID" BIGINT,
        "SensorID" VARCHAR(24),
        "KeyValue" VARCHAR(32),
        "LowerEdge" NUMERIC,
        "UpperEdge" NUMERIC,
        "FormalLanguage" TEXT,
        "Type" INT
    )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    DELETE FROM "Triggers"
    WHERE "Triggers"."ID" = id
	RETURNING
        "Triggers"."ID",
	    "Triggers"."SensorID",
	    "Triggers"."KeyValue",
	    "Triggers"."LowerEdge",
	    "Triggers"."UpperEdge",
	    "Triggers"."FormalLanguage",
	    "Triggers"."Type";
END
$$
CREATE FUNCTION networkapi_deletetriggersbysensorid(sensorid VARCHAR(24))
    RETURNS TABLE(
        "ID" BIGINT,
        "SensorID" VARCHAR(24),
        "KeyValue" VARCHAR(32),
        "LowerEdge" NUMERIC,
        "UpperEdge" NUMERIC,
        "FormalLanguage" TEXT,
        "Type" INT
    )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    DELETE FROM "Triggers"
    WHERE "Triggers"."SensorID" = sensorid
	RETURNING
        "Triggers"."ID",
	    "Triggers"."SensorID",
	    "Triggers"."KeyValue",
	    "Triggers"."LowerEdge",
	    "Triggers"."UpperEdge",
	    "Triggers"."FormalLanguage",
	    "Triggers"."Type";
END
$$
CREATE FUNCTION networkapi_selectapikeybykey(key TEXT)
    RETURNS TABLE(
        "UserId" UUID,
        "Revoked" BOOLEAN,
        "Type" INT,
        "ReadOnly" BOOLEAN
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    SELECT "ApiKeys"."UserId"::UUID,
           "ApiKeys"."Revoked",
           "ApiKeys"."Type",
           "ApiKeys"."ReadOnly"
    FROM "ApiKeys"
    WHERE "ApiKey" = key;
END;
$$
CREATE FUNCTION networkapi_selectsensorlinkbysensorid(sensorid VARCHAR(24))
    RETURNS TABLE(
        "SensorID" VARCHAR(24),
        "UserID" UUID
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
    SELECT "SensorLinks"."SensorId"::VARCHAR(24),
           "SensorLinks"."UserId"::UUID
    FROM "SensorLinks"
    WHERE "SensorLinks"."SensorId" = sensorid;
END
$$
CREATE FUNCTION networkapi_selectsensorlinkbyuserid(userid UUID)
    RETURNS TABLE(
        "SensorID" VARCHAR(24),
        "UserID" UUID
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
    SELECT "SensorLinks"."SensorId"::VARCHAR(24),
           "SensorLinks"."UserId"::UUID
    FROM "SensorLinks"
    WHERE "SensorLinks"."UserId" = userid::TEXT;
END
$$
CREATE FUNCTION networkapi_selecttriggerbyid(id BIGINT)
    RETURNS TABLE(
        "ID" BIGINT,
        "SensorID" VARCHAR(24),
        "KeyValue" VARCHAR(32),
        "LowerEdge" NUMERIC,
        "UpperEdge" NUMERIC,
        "FormalLanguage" TEXT,
        "Type" INT,
        "ActionID" BIGINT,
        "Channel" INT,
        "Target" VARCHAR(255),
        "Message" TEXT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
    SELECT
        "Triggers"."ID",
        "Triggers"."SensorID",
        "Triggers"."KeyValue",
        "Triggers"."LowerEdge",
        "Triggers"."UpperEdge",
        "Triggers"."FormalLanguage",
        "Triggers"."Type",
        "ta"."ID" AS "ActionID",
        "ta"."Channel",
        "ta"."Target",
        "ta"."Message"
    FROM "Triggers"
    LEFT JOIN "TriggerActions" AS ta ON ta."TriggerID" = "Triggers"."ID"
	WHERE "Triggers"."ID" = id;
END
$$
drop function networkapi_selecttriggerbysensorid;

CREATE FUNCTION networkapi_selecttriggerbysensorid(sensorid VARCHAR(24))
    RETURNS TABLE(
        "ID" BIGINT,
        "SensorID" VARCHAR(24),
        "KeyValue" VARCHAR(32),
        "LowerEdge" NUMERIC,
        "UpperEdge" NUMERIC,
        "FormalLanguage" TEXT,
        "Type" INT,
        "ActionID" BIGINT,
        "Channel" INT,
        "Target" VARCHAR(255),
        "Message" TEXT
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
    SELECT
        "Triggers"."ID",
        "Triggers"."SensorID",
        "Triggers"."KeyValue",
        "Triggers"."LowerEdge",
        "Triggers"."UpperEdge",
        "Triggers"."FormalLanguage",
        "Triggers"."Type",
        "ta"."ID" AS "ActionID",
        "ta"."Channel",
        "ta"."Target",
        "ta"."Message"
    FROM "Triggers"
    LEFT JOIN "TriggerActions" AS ta ON ta."TriggerID" = "Triggers"."ID"
	WHERE "Triggers"."SensorID" = sensorid
    ORDER BY "Triggers"."ID", ta."ID";
END
$$
CREATE FUNCTION networkapi_selectuserbyemail(email TEXT)
    RETURNS TABLE(
        "ID" UUID,
        "Firstname" TEXT,
        "Lastname" TEXT,
        "Email" VARCHAR(256),
        "RegisteredAt" TIMESTAMP,
        "PhoneNumber" TEXT,
        "BillingLockout" BOOLEAN,
        "Role" VARCHAR(256)
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    SELECT "Users"."Id"::UUID AS "ID",
           "Users"."FirstName",
           "Users"."LastName",
           "Users"."Email",
           "Users"."RegisteredAt",
           "Users"."PhoneNumber",
           "Users"."BillingLockout",
           "Roles"."NormalizedName" AS "Role"
    FROM "Users"
    INNER JOIN "UserRoles" ON "Users"."Id" = "UserRoles"."UserId"
    JOIN "Roles" ON "UserRoles"."RoleId" = "Roles"."Id"
    WHERE "Users"."Email" = email;
END;
$$
CREATE FUNCTION networkapi_selectuserbyid(userid UUID)
    RETURNS TABLE(
        "ID" UUID,
        "Firstname" TEXT,
        "Lastname" TEXT,
        "Email" VARCHAR(256),
        "RegisteredAt" TIMESTAMP,
        "PhoneNumber" TEXT,
        "BillingLockout" BOOLEAN,
        "Role" VARCHAR(256)
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY

    SELECT "Users"."Id"::UUID AS "ID",
           "Users"."FirstName",
           "Users"."LastName",
           "Users"."Email",
           "Users"."RegisteredAt",
           "Users"."PhoneNumber",
           "Users"."BillingLockout",
           "Roles"."NormalizedName" AS "Role"
    FROM "Users"
    INNER JOIN "UserRoles" ON "Users"."Id" = "UserRoles"."UserId"
    JOIN "Roles" ON "UserRoles"."RoleId" = "Roles"."Id"
    WHERE "Users"."Id" = userid::TEXT;
END;
$$

CREATE FUNCTION networkapi_selectusersbyid(userids TEXT)
    RETURNS TABLE(
        "ID" UUID,
        "Firstname" TEXT,
        "Lastname" TEXT,
        "Email" VARCHAR(256),
        "RegisteredAt" TIMESTAMP,
        "PhoneNumber" TEXT,
        "BillingLockout" BOOLEAN,
        "Role" VARCHAR(256)
                 )
    LANGUAGE plpgsql
AS $$
DECLARE idlist VARCHAR(36)[];
BEGIN
    idlist = ARRAY(SELECT DISTINCT UNNEST(string_to_array(userids, ',')));
	RETURN QUERY

    SELECT "Users"."Id"::UUID AS "ID",
           "Users"."FirstName",
           "Users"."LastName",
           "Users"."Email",
           "Users"."RegisteredAt",
           "Users"."PhoneNumber",
           "Users"."BillingLockout",
           "Roles"."NormalizedName" AS "Role"
    FROM "Users"
    INNER JOIN "UserRoles" ON "Users"."Id" = "UserRoles"."UserId"
    JOIN "Roles" ON "UserRoles"."RoleId" = "Roles"."Id"
    WHERE "Users"."Id" = ANY(idlist);
END;
$$
CREATE FUNCTION networkapi_updateapikey(old TEXT, new TEXT)
    RETURNS TABLE(
        "Id" UUID,
        "UserId" UUID,
        "ApiKey" TEXT,
        "Revoked" BOOLEAN,
        "Type" INT,
        "ReadOnly" BOOLEAN
                 )
    LANGUAGE plpgsql
AS $$
BEGIN
	RETURN QUERY
	
	UPDATE "ApiKeys"
	SET	"ApiKey" = new
	WHERE "ApiKeys"."ApiKey" = old
    RETURNING
        "ApiKeys"."Id"::UUID,
        "ApiKeys"."UserId"::UUID,
        "ApiKeys"."ApiKey",
        "ApiKeys"."Revoked",
        "ApiKeys"."Type",
        "ApiKeys"."ReadOnly";
END;
$$

CREATE FUNCTION router_getaccount(userid UUID)
    RETURNS TABLE(id UUID, billinglockout BOOLEAN, banned BOOLEAN)
    LANGUAGE plpgsql
AS
$$
BEGIN
RETURN query
    SELECT "Users"."Id"::uuid,
           "BillingLockout",
           "Roles"."NormalizedName" = 'BANNED' AS "Banned"
    FROM "Users"
    INNER JOIN "UserRoles" ON "Users"."Id" = "UserRoles"."UserId"
    INNER JOIN "Roles" ON "UserRoles"."RoleId" = "Roles"."Id"
	WHERE "Users"."Id" = userid::text
    GROUP BY "Users"."Id", "BillingLockout", "Roles"."NormalizedName" = 'BANNED';
	
END
$$;

CREATE FUNCTION router_getaccounts()
    RETURNS TABLE(id UUID, billinglockout BOOLEAN, banned BOOLEAN)
    LANGUAGE plpgsql
AS
$$
BEGIN
RETURN query
    SELECT "Users"."Id"::uuid,
           "BillingLockout",
           "Roles"."NormalizedName" = 'BANNED' AS "Banned"
    FROM "Users"
    INNER JOIN "UserRoles" ON "Users"."Id" = "UserRoles"."UserId"
    INNER JOIN "Roles" ON "UserRoles"."RoleId" = "Roles"."Id"
    GROUP BY "Users"."Id", "BillingLockout", "Roles"."NormalizedName" = 'BANNED';
END
$$;
---
--- Get the active live data handlers.
---

CREATE FUNCTION router_getlivedatahandlers()
	RETURNS TABLE(
		"Name" VARCHAR(64)
	)
	LANGUAGE PLPGSQL
AS
$$
BEGIN
	RETURN QUERY
	SELECT ldh."Name"
	FROM "LiveDataHandlers" AS ldh
	WHERE ldh."Enabled" = True;
END;
$$
CREATE FUNCTION router_getsensorkey(sensorkey text)
    RETURNS TABLE(apikey text, userid uuid, revoked boolean, readonly boolean)
	LANGUAGE plpgsql
AS
$$
BEGIN
RETURN query
	SELECT "ApiKey",
		   "UserId"::uuid,
		   "Revoked",
		   "ReadOnly"
	FROM "ApiKeys"
	WHERE "Type" = 0 AND "ApiKey" = sensorkey;
END;
$$;

CREATE FUNCTION router_getsensorkeys()
    RETURNS TABLE(apikey text, userid uuid, revoked boolean, readonly boolean)
	LANGUAGE plpgsql
AS
$$
BEGIN
RETURN query
	SELECT "ApiKey",
		   "UserId"::uuid,
		   "Revoked",
		   "ReadOnly"
	FROM "ApiKeys"
	WHERE "Type" = 0;
END;
$$;
---
--- Routing trigger lookup function.
---
--- @author Michel Megens
--- @email  michel@michelmegens.net
---

CREATE FUNCTION router_gettriggers()
    RETURNS TABLE(
        "SensorID" varchar(24),
        "ActionCount" BIGINT,
        "TextTrigger" BOOLEAN
    )
    LANGUAGE plpgsql
AS
$$
BEGIN
	RETURN QUERY
	SELECT "Triggers"."SensorID",
		   COUNT("TriggerActions"."ID") AS "ActionCount",
		   "Triggers"."FormalLanguage" IS NOT NULL AS "TextTrigger"
	FROM "Triggers"
	LEFT JOIN "TriggerActions" ON "Triggers"."ID" = "TriggerActions"."TriggerID"
	GROUP BY "Triggers"."SensorID", "Triggers"."FormalLanguage";
end;
$$;
---
--- Lookup routing trigger info by ID.
---
--- @author Michel Megens
--- @email  michel@michelmegens.net
---

CREATE FUNCTION router_gettriggersbyid(id varchar(24))
    RETURNS TABLE(
        "SensorID" varchar(24),
        "ActionCount" BIGINT,
        "TextTrigger" BOOLEAN
    )
    LANGUAGE plpgsql
AS
$$
BEGIN
    RETURN QUERY
    SELECT "Triggers"."SensorID",
           COUNT("TriggerActions"."ID") AS "ActionCount",
           "Triggers"."FormalLanguage" IS NOT NULL AS "TextTrigger"
    FROM "Triggers"
    LEFT JOIN "TriggerActions" ON "Triggers"."ID" = "TriggerActions"."TriggerID"
    WHERE "Triggers"."SensorID" = id
    GROUP BY "Triggers"."SensorID", "Triggers"."FormalLanguage";
 end;
$$;
---
--- Select triggers and last invocations based.
---
--- @author Michel Megens
--- @email  michel@michelmegens.net
---

CREATE FUNCTION triggerservice_gettriggersbysensorid(idlist TEXT)
    RETURNS TABLE(
		"TriggerID" BIGINT,
		"ActionID" BIGINT,
		"SensorID" VARCHAR(24),
		"KeyValue" VARCHAR(32),
		"LowerEdge" NUMERIC,
		"UpperEdge" NUMERIC,
		"FormalLanguage" TEXT,
		"Type" INTEGER,
		"Channel" INTEGER,
		"Target" VARCHAR(255),
		"Message" TEXT,
		"LastInvocation" TIMESTAMP
    )
    LANGUAGE plpgsql
AS
$$
DECLARE sensorIds VARCHAR(24)[];
BEGIN
	sensorIds = ARRAY(SELECT DISTINCT UNNEST(string_to_array(idlist, ',')));
	
	RETURN QUERY
	SELECT 
		DISTINCT ON (ta."ID")
		t."ID" AS "TriggerID",
		ta."ID" AS "ActionID",
		t."SensorID",
		t."KeyValue",
		t."LowerEdge",
		t."UpperEdge",
		t."FormalLanguage",
		t."Type",
		ta."Channel",
		ta."Target",
		ta."Message",
		inv."Timestamp"
	FROM "TriggerActions" AS ta
	INNER JOIN "Triggers" AS t ON t."ID" = ta."TriggerID"
	LEFT JOIN (
		SELECT tinv."ActionID", MAX(tinv."Timestamp") AS "Timestamp"
		FROM "TriggerInvocations" AS tinv
		GROUP BY tinv."ActionID"
	) inv ON inv."ActionID" = ta."ID"
	WHERE t."SensorID" = ANY (sensorIds)
	ORDER BY ta."ID";
END;
$$;

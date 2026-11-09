-- Idempotent: runs on every startup.

CREATE TABLE IF NOT EXISTS users (
    id            uuid         PRIMARY KEY,
    username      varchar(50)  NOT NULL,
    password_hash text         NOT NULL,
    created_at    timestamptz  NOT NULL
);

-- One account per username regardless of case ("Admin" = "admin").
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_username ON users (lower(username));

CREATE TABLE IF NOT EXISTS releases (
    id           uuid          PRIMARY KEY,
    title        varchar(200)  NOT NULL,
    artist       varchar(200)  NOT NULL,
    release_year integer       NOT NULL,
    format       varchar(10)   NOT NULL CHECK (format IN ('Single', 'EP', 'Album')),
    created_at   timestamptz   NOT NULL,
    updated_at   timestamptz   NOT NULL
);

-- The list is always ordered newest first.
CREATE INDEX IF NOT EXISTS ix_releases_created_at ON releases (created_at DESC, id DESC);

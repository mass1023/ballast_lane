CREATE TABLE IF NOT EXISTS users (
    id          UUID PRIMARY KEY,
    username    VARCHAR(50) NOT NULL UNIQUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS tasks (
    id              UUID PRIMARY KEY,
    title           VARCHAR(200) NOT NULL,
    description     VARCHAR(2000),
    status          VARCHAR(20) NOT NULL CHECK (status IN ('Pending', 'InProgress', 'Done')),
    due_date        DATE NOT NULL,
    responsable_id  UUID NOT NULL REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_tasks_responsable_id ON tasks (responsable_id);
CREATE INDEX IF NOT EXISTS ix_tasks_status ON tasks (status);

INSERT INTO users (id, username) VALUES
    ('11111111-1111-1111-1111-111111111111', 'alice'),
    ('22222222-2222-2222-2222-222222222222', 'bob'),
    ('33333333-3333-3333-3333-333333333333', 'carol')
ON CONFLICT (id) DO NOTHING;

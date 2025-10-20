CREATE TABLE IF NOT EXISTS telemetry (
    id UUID PRIMARY KEY,
    device_id TEXT NOT NULL,
    is_on BOOLEAN NOT NULL,
    location TEXT NOT NULL,
    type TEXT NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL
);
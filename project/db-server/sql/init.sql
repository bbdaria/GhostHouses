CREATE SCHEMA IF NOT EXISTS bootstrap;

CREATE TABLE IF NOT EXISTS bootstrap."LegacyBuildings" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Street" TEXT NOT NULL,
    "Status" TEXT NOT NULL DEFAULT 'survey-needed'
);

INSERT INTO bootstrap."LegacyBuildings" ("Name", "Street", "Status") VALUES
    ('Haunted Manor', 'Herzl 5', 'survey-needed'),
    ('Crimson Cottage', 'Ben Gurion 12', 'under-review')
ON CONFLICT DO NOTHING;

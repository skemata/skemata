-- Sample data for app-settings-db.skemata.yaml.
-- The table definitions are shown for context only; Skemata schemas do not create tables.

CREATE TABLE settings (
  key   TEXT PRIMARY KEY,
  value TEXT
);

CREATE TABLE shortcuts (
  context TEXT NOT NULL,
  command TEXT NOT NULL,
  keys    TEXT NOT NULL,
  enabled INTEGER NOT NULL DEFAULT 1,
  PRIMARY KEY (context, command)
);

CREATE TABLE sync_state (
  device_id TEXT PRIMARY KEY,
  last_sync TEXT NOT NULL,
  revision  TEXT NOT NULL
);

INSERT INTO settings VALUES
  ('theme', 'dark'),
  ('language', 'de-DE'),
  ('proxy', NULL);

INSERT INTO shortcuts VALUES
  ('editor', 'save', 'Ctrl+S', 1),
  ('global', 'search', 'Ctrl+Shift+F', 1),
  ('terminal', 'clear', 'Ctrl+L', 0);

INSERT INTO sync_state VALUES
  ('1b4e28ba-2fa1-11d2-883f-0016d3cca427', '2026-10-01T08:15:00Z', '42');

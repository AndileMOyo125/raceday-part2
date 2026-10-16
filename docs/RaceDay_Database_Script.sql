-- RaceDay Database Script
-- Replace this with your actual SQL database schema and setup script

-- Placeholder database schema for RaceDay application
CREATE TABLE IF NOT EXISTS races (
    race_id INT PRIMARY KEY,
    race_name VARCHAR(255) NOT NULL,
    race_date DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS participants (
    participant_id INT PRIMARY KEY,
    race_id INT NOT NULL,
    participant_name VARCHAR(255) NOT NULL,
    FOREIGN KEY (race_id) REFERENCES races(race_id)
);

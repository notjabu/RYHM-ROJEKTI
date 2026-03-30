Kyseinen tietokanta salasanalla YES123

---------------------------------------------------------------------------------------------

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0; 

SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0; 

SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION'; 

  

DROP SCHEMA IF EXISTS `vn`; 

CREATE SCHEMA IF NOT EXISTS `vn` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_bin; 

USE `vn`; 

  

-- ----------------------------------------------------- 

-- Tables (unchanged) 

-- ----------------------------------------------------- 

CREATE TABLE IF NOT EXISTS `alue` ( 

  `alue_id` INT UNSIGNED NOT NULL AUTO_INCREMENT, 

  `nimi` VARCHAR(40) NULL DEFAULT NULL, 

  `sijainti` VARCHAR(45) NULL DEFAULT NULL, 

  `kuvaus` VARCHAR(45) NULL DEFAULT NULL, 

  PRIMARY KEY (`alue_id`), 

  INDEX `alue_nimi_index` (`nimi` ASC) VISIBLE 

) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin; 

  

CREATE TABLE IF NOT EXISTS `posti` ( 

  `postinro` CHAR(5) NOT NULL, 

  `toimipaikka` VARCHAR(45) NULL DEFAULT NULL, 

  PRIMARY KEY (`postinro`) 

) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin; 

  

CREATE TABLE IF NOT EXISTS `asiakas` ( 

  `asiakas_id` INT UNSIGNED NOT NULL AUTO_INCREMENT, 

  `postinro` CHAR(5) NOT NULL, 

  `etunimi` VARCHAR(20) NULL DEFAULT NULL, 

  `sukunimi` VARCHAR(40) NULL DEFAULT NULL, 

  `lahiosoite` VARCHAR(40) NULL DEFAULT NULL, 

  `email` VARCHAR(50) NULL DEFAULT NULL, 

  `puhelinnro` VARCHAR(15) NULL DEFAULT NULL, 

  PRIMARY KEY (`asiakas_id`), 

  INDEX `fk_as_posti1_idx` (`postinro` ASC) VISIBLE, 

  INDEX `asiakas_snimi_idx` (`sukunimi` ASC) VISIBLE, 

  INDEX `asiakas_enimi_idx` (`etunimi` ASC) VISIBLE, 

  CONSTRAINT `fk_asiakas_posti` FOREIGN KEY (`postinro`) REFERENCES `posti` (`postinro`) 

) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin; 

  

CREATE TABLE IF NOT EXISTS `mokki` ( 

  `mokki_id` INT UNSIGNED NOT NULL AUTO_INCREMENT, 

  `alue_id` INT UNSIGNED NOT NULL, 

  `postinro` CHAR(5) NOT NULL, 

  `mokkinimi` VARCHAR(45) NULL DEFAULT NULL, 

  `katuosoite` VARCHAR(45) NULL DEFAULT NULL, 

  `hinta` DOUBLE(8,2) NOT NULL, 

  `kuvaus` VARCHAR(150) NULL DEFAULT NULL, 

  `henkilomaara` INT NULL DEFAULT NULL, 

  `varustelu` VARCHAR(100) NULL DEFAULT NULL, 

  PRIMARY KEY (`mokki_id`), 

  INDEX `fk_mokki_alue_idx` (`alue_id` ASC) VISIBLE, 

  INDEX `fk_mokki_posti_idx` (`postinro` ASC) VISIBLE, 

  CONSTRAINT `fk_mokki_alue` FOREIGN KEY (`alue_id`) REFERENCES `alue` (`alue_id`), 

  CONSTRAINT `fk_mokki_posti` FOREIGN KEY (`postinro`) REFERENCES `posti` (`postinro`) 

) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin; 

  

CREATE TABLE IF NOT EXISTS `varaus` ( 

  `varaus_id` INT UNSIGNED NOT NULL AUTO_INCREMENT, 

  `asiakas_id` INT UNSIGNED NOT NULL, 

  `mokki_id` INT UNSIGNED NOT NULL, 

  `varattu_pvm` DATETIME NULL DEFAULT NULL, 

  `vahvistus_pvm` DATETIME NULL DEFAULT NULL, 

  `varattu_alkupvm` DATETIME NULL DEFAULT NULL, 

  `varattu_loppupvm` DATETIME NULL DEFAULT NULL, 

  PRIMARY KEY (`varaus_id`), 

  INDEX `varaus_as_id_index` (`asiakas_id` ASC) VISIBLE, 

  INDEX `fk_var_mok_idx` (`mokki_id` ASC) VISIBLE, 

  CONSTRAINT `fk_varaus_mokki` FOREIGN KEY (`mokki_id`) REFERENCES `mokki` (`mokki_id`), 

  CONSTRAINT `varaus_ibfk` FOREIGN KEY (`asiakas_id`) REFERENCES `asiakas` (`asiakas_id`) 

) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin; 

  

CREATE TABLE IF NOT EXISTS `lasku` ( 

  `lasku_id` INT NOT NULL, 

  `varaus_id` INT UNSIGNED NOT NULL, 

  `summa` DOUBLE(8,2) NOT NULL, 

  `alv` DOUBLE(8,2) NOT NULL, 

  `maksettu` DOUBLE(8,2) NOT NULL DEFAULT '0.00', 

  PRIMARY KEY (`lasku_id`), 

  INDEX `lasku_ibfk_1` (`varaus_id` ASC) VISIBLE, 

  CONSTRAINT `lasku_ibfk_1` FOREIGN KEY (`varaus_id`) REFERENCES `varaus` (`varaus_id`) 

) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin; 

  

CREATE TABLE IF NOT EXISTS `palvelu` ( 

  `palvelu_id` INT UNSIGNED NOT NULL, 

  `alue_id` INT UNSIGNED NOT NULL, 

  `nimi` VARCHAR(40) NULL DEFAULT NULL, 

  `kuvaus` VARCHAR(255) NULL DEFAULT NULL, 

  `hinta` DOUBLE(8,2) NOT NULL, 

  `alv` DOUBLE(8,2) NOT NULL, 

  PRIMARY KEY (`palvelu_id`), 

  INDEX `Palvelu_nimi_index` (`nimi` ASC) VISIBLE, 

  INDEX `palv_toimip_id_ind` (`alue_id` ASC) VISIBLE, 

  CONSTRAINT `palvelu_ibfk_1` FOREIGN KEY (`alue_id`) REFERENCES `alue` (`alue_id`) 

) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin; 

  

CREATE TABLE IF NOT EXISTS `varauksen_palvelut` ( 

  `varaus_id` INT UNSIGNED NOT NULL, 

  `palvelu_id` INT UNSIGNED NOT NULL, 

  `lkm` INT NOT NULL, 

  PRIMARY KEY (`palvelu_id`, `varaus_id`), 

  INDEX `vp_varaus_id_index` (`varaus_id` ASC) VISIBLE, 

  INDEX `vp_palvelu_id_index` (`palvelu_id` ASC) VISIBLE, 

  CONSTRAINT `fk_palvelu` FOREIGN KEY (`palvelu_id`) REFERENCES `palvelu` (`palvelu_id`), 

  CONSTRAINT `fk_varaus` FOREIGN KEY (`varaus_id`) REFERENCES `varaus` (`varaus_id`) 

) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_bin; 

  

-- ----------------------------------------------------- 

-- UPDATED INSERT SAMPLE DATA 

-- ----------------------------------------------------- 

  

-- alue (unchanged) 

INSERT INTO `alue` (`nimi`, `sijainti`, `kuvaus`) VALUES 

('Ruka', 'Kuusamo', 'Laskettelukeskus ja talviaktiviteetit'), 

('Tahko', 'Nilsiä', 'Laskettelukeskus Itä-Suomessa'), 

('Ylläs', 'Kolari', 'Laskettelukeskus Lapissa'); 

  

-- posti (unchanged) 

INSERT INTO `posti` (`postinro`, `toimipaikka`) VALUES 

('00100', 'Helsinki'), 

('90100', 'Oulu'), 

('70100', 'Kuopio'), 

('93600', 'Kuusamo'), 

('73300', 'Nilsiä'), 

('95900', 'Kolari'), 

('20100', 'Turku'); 

  

-- asiakas (unchanged) 

INSERT INTO `asiakas` (`postinro`, `etunimi`, `sukunimi`, `lahiosoite`, `email`, `puhelinnro`) VALUES 

('00100', 'Matti', 'Meikäläinen', 'Keskuskatu 5', 'matti.meikalainen@example.com', '0401234567'), 

('90100', 'Liisa', 'Virtanen', 'Rantakatu 22', 'liisa.virtanen@example.com', '0509876543'), 

('70100', 'Jukka', 'Korhonen', 'Kuusitie 10', 'jukka.k@example.com', '0412233445'); 

  

-- mokki (unchanged) 

INSERT INTO `mokki` (`alue_id`, `postinro`, `mokkinimi`, `katuosoite`, `hinta`, `kuvaus`, `henkilomaara`, `varustelu`) VALUES 

(1, '90100', 'Rukamökki', 'Rukatunturi 5', 250.00, 'Luksusmökki Rukalla', 6, 'Sauna, takka, wifi'), 

(2, '70100', 'Tahkomökki', 'Tahkonrinne 3', 180.00, 'Mökki Tahkon laskettelukeskuksessa', 4, 'Sauna, grilli, vene'), 

(3, '20100', 'Ylläsmökki', 'Yllästunturi 8', 300.00, 'Merenrantamökki Ylläksellä', 8, 'Ulkoporeallas, sauna, aurinkoterassi'); 

  

-- varaus (unchanged) 

INSERT INTO `varaus` (`asiakas_id`, `mokki_id`, `varattu_pvm`, `vahvistus_pvm`, `varattu_alkupvm`, `varattu_loppupvm`) VALUES 

(1, 1, '2024-01-10 09:00:00', '2024-01-10 10:00:00', '2024-02-15 15:00:00', '2024-02-20 12:00:00'), 

(2, 2, '2024-03-01 11:30:00', '2024-03-01 13:00:00', '2024-04-01 14:00:00', '2024-04-05 12:00:00'), 

(3, 3, '2024-05-15 08:45:00', '2024-05-15 09:15:00', '2024-06-10 16:00:00', '2024-06-14 12:00:00'); 

  

-- lasku (unchanged) 

INSERT INTO `lasku` (`lasku_id`, `varaus_id`, `summa`, `alv`, `maksettu`) VALUES 

(1, 1, 1250.00, 24.00, 1250.00), 

(2, 2, 900.00, 24.00, 0.00), 

(3, 3, 1200.00, 24.00, 1200.00); 

  

-- palvelu → NOW INCLUDES airsoft AND hevosajelu (exactly as requested) 

INSERT INTO `palvelu` (`palvelu_id`, `alue_id`, `nimi`, `kuvaus`, `hinta`, `alv`) VALUES 

(1, 1, 'Porosafari', 'Opastettu porosafari Rukalla', 150.00, 24.00), 

(2, 2, 'Koiravaljakkoajelu', 'Koiravaljakkoajelu Tahkolla', 140.00, 24.00), 

(3, 3, 'Vesiskootteriajelu', 'Vesiskootteriajelu Ylläksellä (kesä)', 90.00, 24.00), 

(4, 1, 'Airsoft', 'Airsoft-taistelu Rukalla', 85.00, 24.00), 

(5, 2, 'Hevosajelu', 'Hevosajelu Tahkolla', 75.00, 24.00); 

  

-- varauksen_palvelut (unchanged - still references the original 3 services) 

INSERT INTO `varauksen_palvelut` (`varaus_id`, `palvelu_id`, `lkm`) VALUES 

(1, 1, 2), 

(2, 2, 1), 

(3, 3, 4); 

  

SET SQL_MODE=@OLD_SQL_MODE; 

SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS; 

SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS; 

CREATE DATABASE  IF NOT EXISTS `animalshelter` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `animalshelter`;
-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: localhost    Database: animalshelter
-- ------------------------------------------------------
-- Server version	8.0.45

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `account`
--

DROP TABLE IF EXISTS `account`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account` (
  `id` int NOT NULL AUTO_INCREMENT,
  `email` varchar(100) NOT NULL,
  `password` varchar(255) NOT NULL,
  `registrationDate` datetime DEFAULT CURRENT_TIMESTAMP,
  `role` enum('user','shelter_admin','system_admin') NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `account`
--

LOCK TABLES `account` WRITE;
/*!40000 ALTER TABLE `account` DISABLE KEYS */;
INSERT INTO `account` VALUES (1,'karpiakmarta23@gmail.com','pass123','2026-03-14 00:15:32','user'),(2,'pzakutynskyi@gmail.com','pass123','2026-03-14 00:15:32','user'),(3,'haroqub@gmail.com','pass123','2026-03-14 00:15:32','user'),(4,'skorobogatuh.dara@gmail.com','pass123','2026-03-14 00:15:32','user'),(5,'diana.magotska@petly.com','powershell Compress-Archive -Path Petly_2term -DestinationPath Petly_2term.zip','2026-03-14 00:15:32','user'),(6,'happy.paws.lviv@gmail.com','pass123','2026-03-14 00:15:32','shelter_admin'),(7,'domivka.lviv@gmail.com','pass123','2026-03-14 00:15:32','shelter_admin'),(8,'animal.rescue.ua@gmail.com','pass123','2026-03-14 00:15:32','shelter_admin'),(9,'pets.home.kyiv@gmail.com','pass123','2026-03-14 00:15:32','shelter_admin'),(10,'admin@petly.com','admin123','2026-03-14 00:15:32','system_admin');
/*!40000 ALTER TABLE `account` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `adoptionapplication`
--

DROP TABLE IF EXISTS `adoptionapplication`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `adoptionapplication` (
  `adoptId` int NOT NULL AUTO_INCREMENT,
  `userId` int NOT NULL,
  `petId` int NOT NULL,
  `status` enum('Pending','Approved','Rejected','Очікує','Схвалено','Відхилено', 'Авто-відхилено') DEFAULT NULL,
  `submissionDate` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`adoptId`),
  KEY `userId` (`userId`),
  KEY `petId` (`petId`),
  CONSTRAINT `adoptionapplication_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`accountId`),
  CONSTRAINT `adoptionapplication_ibfk_2` FOREIGN KEY (`petId`) REFERENCES `pet` (`petId`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `adoptionapplication`
--

LOCK TABLES `adoptionapplication` WRITE;
/*!40000 ALTER TABLE `adoptionapplication` DISABLE KEYS */;
INSERT INTO `adoptionapplication` VALUES (6,1,21,'Очікує','2026-03-14 00:35:17'),(7,2,24,'Схвалено','2026-03-14 00:35:17'),(8,3,27,'Очікує','2026-03-14 00:35:17'),(9,4,23,'Відхилено','2026-03-14 00:35:17'),(10,5,28,'Очікує','2026-03-14 00:35:17');
/*!40000 ALTER TABLE `adoptionapplication` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `infopage`
--

DROP TABLE IF EXISTS `infopage`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `infopage` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(100) DEFAULT NULL,
  `content` text,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `infopage`
--

LOCK TABLES `infopage` WRITE;
/*!40000 ALTER TABLE `infopage` DISABLE KEYS */;
INSERT INTO `infopage` VALUES (1,'Про Petly','Petly — це платформа, яка поєднує притулки для тварин з людьми, які хочуть усиновити домашніх улюбленців.'),(2,'Процес усиновлення','Перегляньте тварин, оберіть улюбленця та подайте заявку на усиновлення. Притулок розгляне ваш запит.'),(3,'Як допомогти','Ви можете допомогти притулкам, усиновивши тварину або пожертвувавши корм чи інші необхідні речі.'),(4,'Контакти','Зв’яжіться з притулками або адміністратором платформи, якщо хочете підтримати порятунок тварин.');
/*!40000 ALTER TABLE `infopage` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pet`
--

DROP TABLE IF EXISTS `pet`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pet` (
  `petId` int NOT NULL AUTO_INCREMENT,
  `shelterId` int NOT NULL,
  `petName` varchar(100) NOT NULL,
  `type` enum('Dog','Cat','Собака','Кіт') DEFAULT NULL,
  `breed` varchar(100) DEFAULT NULL,
  `gender` enum('Male','Female','Хлопчик','Дівчинка') DEFAULT NULL,
  `age` int DEFAULT NULL,
  `size` enum('Small','Medium','Large','Маленький','Середній','Великий') DEFAULT NULL,
  `vaccinated` tinyint(1) DEFAULT '0',
  `sterilized` tinyint(1) DEFAULT '0',
  `status` enum('Available', 'Доступний', 'Прилаштований') DEFAULT NULL,
  `photoUrl` varchar(255) DEFAULT NULL,
  `description` text,
  `createdAt` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`petId`),
  KEY `shelterId` (`shelterId`),
  CONSTRAINT `pet_ibfk_1` FOREIGN KEY (`shelterId`) REFERENCES `shelter` (`accountId`)
) ENGINE=InnoDB AUTO_INCREMENT=31 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pet`
--

LOCK TABLES `pet` WRITE;
/*!40000 ALTER TABLE `pet` DISABLE KEYS */;
INSERT INTO `pet` VALUES (21,6,'Белла','Собака','Золотистий ретривер','Дівчинка',3,'Великий',1,1,'Доступний','/images/pets/bella.jpg','Дуже дружній золотистий ретривер','2026-03-14 00:32:08'),(22,6,'Макс','Собака','Німецька вівчарка','Хлопчик',5,'Великий',1,1,'Доступний','/images/pets/max.jpg','Вірний та захисний пес','2026-03-14 00:32:08'),(23,6,'Чарлі','Собака','Лабрадор ретривер','Хлопчик',2,'Великий',1,0,'Доступний','/images/pets/charlie.jpg','Енергійний та грайливий','2026-03-14 00:32:08'),(24,7,'Луна','Кіт','Британська короткошерста','Дівчинка',1,'Маленький',1,0,'Доступний','/images/pets/luna.jpg','Грайливе кошеня','2026-03-14 00:32:08'),(25,7,'Міло','Кіт','Мейн-кун','Хлопчик',2,'Середній',1,1,'Доступний','/images/pets/milo.jpg','Спокійний та дружній кіт','2026-03-14 00:32:08'),(26,7,'Сімба','Кіт','Метис','Хлопчик',4,'Середній',1,1,'Доступний','/images/pets/simba.jpg','Дуже лагідний кіт','2026-03-14 00:32:08'),(27,8,'Роккі','Собака','Метис','Хлопчик',6,'Великий',1,1,'Доступний','/images/pets/rocky.jpg','Врятований безпритульний пес','2026-03-14 00:32:08'),(28,8,'Дейзі','Собака','Бігль','Дівчинка',3,'Середній',1,1,'Доступний','/images/pets/daisy.jpg','Ніжний та дружній пес','2026-03-14 00:32:08'),(29,9,'Олівер','Кіт','Шотландська висловуха','Хлопчик',2,'Маленький',1,0,'Доступний','/images/pets/oliver.jpg','Допитливий кіт','2026-03-14 00:32:08'),(30,9,'Люсі','Кіт','Персидська кішка','Дівчинка',5,'Маленький',1,1,'Доступний','/images/pets/lucy.jpg','Дуже спокійний кіт','2026-03-14 00:32:08');
/*!40000 ALTER TABLE `pet` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `shelter`
--

DROP TABLE IF EXISTS `shelter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `shelter` (
  `accountId` int NOT NULL,
  `shelterName` varchar(100) DEFAULT NULL,
  `location` varchar(150) DEFAULT NULL,
  `adminName` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`accountId`),
  CONSTRAINT `shelter_ibfk_1` FOREIGN KEY (`accountId`) REFERENCES `account` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `shelter`
--

LOCK TABLES `shelter` WRITE;
/*!40000 ALTER TABLE `shelter` DISABLE KEYS */;
INSERT INTO `shelter` VALUES (6,'Притулок Щасливі Лапи','Львів','Оксана Мельник'),(7,'Притулок Домівка','Львів','Ірина Коваль'),(8,'Порятунок Тварин Україна','Київ','Сергій Бондаренко'),(9,'Дім для Тварин Київ','Київ','Катерина Гриценко');
/*!40000 ALTER TABLE `shelter` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `shelterneed`
--

DROP TABLE IF EXISTS `shelterneed`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `shelterneed` (
  `needId` int NOT NULL AUTO_INCREMENT,
  `shelterId` int NOT NULL,
  `description` text,
  `paymentDetails` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`needId`),
  KEY `shelterId` (`shelterId`),
  CONSTRAINT `shelterneed_ibfk_1` FOREIGN KEY (`shelterId`) REFERENCES `shelter` (`accountId`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `shelterneed`
--

LOCK TABLES `shelterneed` WRITE;
/*!40000 ALTER TABLE `shelterneed` DISABLE KEYS */;
INSERT INTO `shelterneed` VALUES (1,6,'Потрібні сухий корм та ковдри для собак','Можливий банківський переказ'),(2,7,'Потрібні корм для котів та наповнювач','У притулку є скринька для пожертв'),(3,8,'Потрібні медичні препарати для врятованих тварин','Потрібна ветеринарна допомога'),(4,9,'Потрібні кошти на ремонт притулку','Доступна онлайн-пожертва');
/*!40000 ALTER TABLE `shelterneed` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user`
--

DROP TABLE IF EXISTS `user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user` (
  `accountId` int NOT NULL,
  `name` varchar(100) DEFAULT NULL,
  `surname` varchar(100) DEFAULT NULL,
  `status` enum('Active','Активний') DEFAULT NULL,
  PRIMARY KEY (`accountId`),
  CONSTRAINT `user_ibfk_1` FOREIGN KEY (`accountId`) REFERENCES `account` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user`
--

LOCK TABLES `user` WRITE;
/*!40000 ALTER TABLE `user` DISABLE KEYS */;
INSERT INTO `user` VALUES (1,'Marta','Karpyak','Активний'),(2,'Pavlo','Zakutynskyi','Активний'),(3,'Nataliia','Ivanko','Активний'),(4,'Daryna','Skorobagatykh','Активний'),(5,'Diana','Mahotska','Активний');
/*!40000 ALTER TABLE `user` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-15 18:44:05

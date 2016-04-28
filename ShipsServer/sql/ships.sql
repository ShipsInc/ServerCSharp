DROP TABLE IF EXISTS `statistics`;
CREATE TABLE `statistics` (
  `Id` int(10) unsigned NOT NULL DEFAULT '0',
  `lastBattle` int(11) unsigned DEFAULT '0',
  `wins` smallint(10) unsigned DEFAULT '0',
  `loose` smallint(10) unsigned DEFAULT '0',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

DROP TABLE IF EXISTS `users`;
CREATE TABLE `users` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(255) DEFAULT NULL,
  `password` varchar(255) DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Activity.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Activity.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Activity.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Activity.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Activity.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Activity.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Activity.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Activity.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Book.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Book.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Book.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Book.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Book.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Book.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Book.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Book.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Category.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Category.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Category.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Category.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Category.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Category.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Category.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Category.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Department.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Department.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Department.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Department.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Department.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Department.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Department.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Department.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Faculty.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Faculty.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Faculty.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Faculty.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Faculty.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Faculty.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Faculty.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Faculty.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Mail.Send') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Mail.Send') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Position.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Position.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Position.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Position.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Position.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Position.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Position.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Position.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Role.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Role.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Role.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Role.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Role.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Role.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Role.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Role.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Rules.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Rules.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Rules.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Rules.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Rules.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Rules.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Rules.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Rules.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Semester.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Semester.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Student.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Student.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Student.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Student.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Student.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Student.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'StudentRegistrations.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('StudentRegistrations.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'StudentRegistrations.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('StudentRegistrations.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'StudentRegistrations.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('StudentRegistrations.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'StudentRegistrations.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('StudentRegistrations.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Subject.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Subject.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Tutor.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Tutor.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Tutor.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Tutor.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Tutor.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Tutor.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'Tutor.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('Tutor.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'User.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('User.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'User.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('User.Delete') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'User.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('User.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'User.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('User.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'view.role') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('view.role') END;

-- RegisterAcc Permissions
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'RegisterAcc.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('RegisterAcc.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'RegisterAcc.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('RegisterAcc.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'RegisterAcc.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('RegisterAcc.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'RegisterAcc.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('RegisterAcc.Delete') END;

-- NewStudentAcc Permissions
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'NewStudentAcc.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('NewStudentAcc.View') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'NewStudentAcc.Create') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('NewStudentAcc.Create') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'NewStudentAcc.Edit') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('NewStudentAcc.Edit') END;
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'NewStudentAcc.Delete') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('NewStudentAcc.Delete') END;

-- StudentDatabank Permissions
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Permission_Name = 'StudentDatabank.View') BEGIN INSERT INTO Permissions (Permission_Name) VALUES ('StudentDatabank.View') END;

-- Grant all permissions to Admin Role (Role_Id = 1)
INSERT INTO Role_Permissions (Role_Id, Permission_Id)
SELECT 1, Permission_Id FROM Permissions
WHERE Permission_Id NOT IN (SELECT Permission_Id FROM Role_Permissions WHERE Role_Id = 1);



CREATE VIEW dbo.DEVICES_VIEW
AS
SELECT     TOP (100) PERCENT dbo.DEVICES.DEV_ID, dbo.DEVICES.CODE AS DEV_CODE, dbo.DEVICES.SIL_N_1 AS DEV_SIL_N, dbo.DEVICES.TS AS DEV_TS, 
                      dbo.DEVICES.USR AS DEV_USER, dbo.DEVICES.MME_CODE AS DEV_MME, dbo.GROUPS.GROUP_ID AS GRP_ID, dbo.GROUPS.GROUP_NAME AS GRP_NAME, 
                      dbo.PROFILES.PROF_NAME AS PRF_NAME, dbo.PROFILES.PROF_VERS AS PRF_VER
FROM         dbo.DEVICES INNER JOIN
                      dbo.GROUPS ON dbo.DEVICES.GROUP_ID = dbo.GROUPS.GROUP_ID INNER JOIN
                      dbo.PROFILES ON dbo.DEVICES.PROFILE_ID = dbo.PROFILES.PROF_GUID
WHERE     (dbo.DEVICES.POS = 1)
ORDER BY DEV_CODE

GO
EXECUTE sp_addextendedproperty @name = N'MS_DiagramPane1', @value = N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "DEVICES"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 123
               Right = 194
            End
            DisplayFlags = 280
            TopColumn = 1
         End
         Begin Table = "GROUPS"
            Begin Extent = 
               Top = 1
               Left = 259
               Bottom = 88
               Right = 415
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "PROFILES"
            Begin Extent = 
               Top = 103
               Left = 231
               Bottom = 220
               Right = 387
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 3435
         Alias = 2205
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1710
         Or = 1350
         Or = 1350
      End
   End
End
', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'VIEW', @level1name = N'DEVICES_VIEW';


GO
EXECUTE sp_addextendedproperty @name = N'MS_DiagramPaneCount', @value = 1, @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'VIEW', @level1name = N'DEVICES_VIEW';


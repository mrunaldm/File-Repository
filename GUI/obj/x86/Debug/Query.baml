   M S B A M L   `   `   `  ����B  >WpfApp1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null!clr-namespace:WpfApp1WpfApp1  5          WpfApp1.Query   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      'localclr-namespace:WpfApp1     5      .��5	      � ���#eee  � RowDefinitions  5
   
   �� 5      $��0.25* �6   �� 5      $��* �6   �� 5      $��3* �6   �� 5      $��* �6   .+�5   
   �� -   5         ��Name$ querySelector��5      $��0��6*   .@�5      �� -   5         ��Name$ byName��5      
 � Tag$ Query by FileName��6+   $��Query by FileName��6C   �� 5      -   5        $ 
byCategory��5      $ Query by category��6/   $��Query by category��6G   �� 5      -   5        $ byDependants��5      $ Files without dependants��61   $��Files without dependants��6P   �� 5      -   5        $ 	byVersion��5      $ Query by version number��6.   $��Query by version Number��6L   5      �� 5   
   $��1��6   .+�5      �� -   5         ��Name$ 
inputParam��5      ���#fff$��20��6<   $��10 10 10 10q�6H   $��2 2 2 2q�6]   �� 5      -   5         ��Name$ 	SendQuery��5      $��
Send Query��6(   $	��100��6U   $��1 1 1 20q�6a   $
��Bold"�6s   5      � 5   
   $��2��6     5      �� 5      $��2* �6    �� 5      $��0.75* �6     � ColumnDefinitions 5      �� 5       $��* �6#   �� 5!      $��* �6#   .+�5#      �� -   5         ��Name$ fileList��5#   5   $��0��6   ���#fff$��
10 10 10 0q�6G   �� 5$      -	   5         ��Name$ metadata��5$   R   $��0��6   $��1��6%   $��2��65   $��10q�6F   	 �� Padding$	 2 2 2 2q�6d   $��Right=�6v   $	��130��6�   5%      �� 5&   
   -
   5        $ QueryMessage��5&   #   $��3��6   ���#fff69   5'      5(      
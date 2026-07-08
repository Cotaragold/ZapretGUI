# Neон WPF Стили - Документация

## 📋 Обзор

Полный набор неоновых WPF стилей для современного интерфейса с поддержкой анимаций, glow эффектов и DynamicResource.

## 🎨 Доступные Стили

### Button (Кнопки)

```xaml
<!-- Основная кнопка (Cyan) -->
<Button Content="Primary" Style="{StaticResource NeonPrimaryButtonStyle}" />

<!-- Вторичная кнопка (Purple) -->
<Button Content="Secondary" Style="{StaticResource NeonSecondaryButtonStyle}" />

<!-- Кнопка опасного действия (Red) -->
<Button Content="Delete" Style="{StaticResource NeonDangerButtonStyle}" />
```

**Особенности:**
- ✅ Rounded corners (12px)
- ✅ Neon border (2px)
- ✅ Hover glow эффект (DropShadowEffect)
- ✅ Pressed state с пониженной opacity
- ✅ Плавная анимация масштаба (1.0 → 1.02)
- ✅ Disabled state
- ✅ Цвета через DynamicResource

---

### TextBox (Текстовые поля)

```xaml
<TextBox Text="Enter text..." Style="{StaticResource NeonTextBoxStyle}" />
```

**Особенности:**
- ✅ Тёмный фон (#15152A)
- ✅ Neon border (Cyan по умолчанию)
- ✅ Focus glow эффект с увеличенной толщиной border
- ✅ Hover highlight (Purple)
- ✅ Cyan caret
- ✅ Rounded corners (8px)
- ✅ Padding 10x8

---

### ListBox & ListBoxItem

```xaml
<ListBox Style="{StaticResource NeonListBoxStyle}"
         ItemContainerStyle="{StaticResource NeonListBoxItemStyle}">
    <ListBoxItem Content="Item 1" />
    <ListBoxItem Content="Item 2" />
</ListBox>
```

**Особенности:**
- ✅ Тёмный фон контейнера
- ✅ Hover highlight на items (Cyan)
- ✅ Selected neon outline (Cyan border + glow)
- ✅ Rounded item corners (6px)
- ✅ Subtle shadow на hover

---

### TabControl & TabItem

```xaml
<TabControl Style="{StaticResource NeonTabControlStyle}">
    <TabItem Header="Tab 1" Style="{StaticResource NeonTabItemStyle}">
        <TextBlock Text="Content 1" />
    </TabItem>
    <TabItem Header="Tab 2" Style="{StaticResource NeonTabItemStyle}">
        <TextBlock Text="Content 2" />
    </TabItem>
</TabControl>
```

**Особенности:**
- ✅ Тёмные вкладки (Transparent background)
- ✅ Selected underline neon (Cyan, 3px высота)
- ✅ Underline glow эффект
- ✅ Hover состояние (Purple текст)
- ✅ Разделитель между табами и контентом

---

### Card Style (Border)

```xaml
<Border Style="{StaticResource NeonCardStyle}">
    <StackPanel>
        <TextBlock Text="Card Content" />
    </StackPanel>
</Border>
```

**Особенности:**
- ✅ Padding 14пх
- ✅ Rounded corners (12px)
- ✅ Subtle glow border
- ✅ Dark background
- ✅ Всегда включен subtle эффект

---

## 🎌 DynamicResource Ключи

Все элементы используют DynamicResource для гибкости. Доступные ключи:

| Ключ | Значение | Применение |
|------|----------|-----------|
| `AccentCyanBrush` | #00FFFF | Primary border, selected state |
| `AccentPurpleBrush` | #B400FF | Hover state, secondary |
| `TextPrimaryBrush` | #F5F7FF | Основной текст |
| `TextSecondaryBrush` | #9FA4C8 | Мутированный текст |
| `TextMuted` | #9FA4C8 | Tab inactive text |
| `CardBackgroundBrush` | #101023 | Card/ListBox фон |
| `CardBorderBrush` | #242445 | Card/ListBox border |
| `InputBackgroundBrush` | #15152A | TextBox фон |
| `ItemBackgroundBrush` | #17172D | ListBox item фон |

**Добавлены в NeonColors.xaml:**
```xaml
<Color x:Key="NeonCyan">#FF00FFFF</Color>
<Color x:Key="NeonPurple">#FFB400FF</Color>
<Color x:Key="Bg0">#FF060714</Color>
<Color x:Key="Bg1">#FF101023</Color>
<Color x:Key="TextPrimary">#FFF5F7FF</Color>
<Color x:Key="TextMuted">#FF9FA4C8</Color>
<Color x:Key="BorderNeon">#FF3D3D7D</Color>
```

---

## ✨ Анимации

### Button Hover Animation
- **Тип:** Scale transformation
- **Диапазон:** 1.0 → 1.02
- **Длительность:** 150ms
- **Применяется:** При IsMouseOver = True

### Glow Effects
- **Постоянно включены:** Card, TabItem underline (subtle)
- **На hover/focus:** Button, TextBox, ListBoxItem
- **BlurRadius:** 8-14px (зависит от элемента)
- **Opacity:** 0.4-0.8

---

## ⚙️ Размеры и Padding

| Элемент | CornerRadius | Padding | Border |
|---------|------------|---------|--------|
| Button | 12px | 12x8 | 2px |
| TextBox | 8px | 10x8 | 1.5px (focus: 2px) |
| ListBox | 8px | 0 | 1.5px |
| ListBoxItem | 6px | 10x8 | 0 (selected: 1.5px) |
| TabItem | - | 14x12 | Variable |
| Card | 12px | 14x14 | 1.5px |

---

## 📌 Требования

- ✅ WPF Framework 4.7.2+
- ✅ Visual Studio 2019+
- ✅ Использование DynamicResource для поддержки runtime смены тем

## 🚀 Использование

### Применить стиль к элементу:
```xaml
<Button Style="{StaticResource NeonPrimaryButtonStyle}" 
        Content="Click Me!" />
```

### Применить к ResourceDictionary (автоматично):
Стили автоматически подключены через `App.xaml`:
```xaml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Theme/NeonColors.xaml" />
    <ResourceDictionary Source="Theme/NeonControls.xaml" />
</ResourceDictionary.MergedDictionaries>
```

---

## 🔧 Кастомизация

### Изменить свечение (glow):
Найдите нужный стиль и отредактируйте `DropShadowEffect`:
```xaml
<DropShadowEffect Color="#00FFFF"
                  BlurRadius="14"
                  ShadowDepth="0"
                  Opacity="0.7" />
```

### Отключить анимации:
Удалите `<Trigger.EnterActions>` и `<Trigger.ExitActions>` в Button template

### Изменить цвета:
Обновите нужные Color или Brush в `NeonColors.xaml`

---

## 📱 Тестирование

Создайте простой тестовый XAML для проверки всех стилей:

```xaml
<Window x:Class="ZapretGUI.ThemeTestWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="Neon Styles Test" Height="600" Width="800"
        Background="{DynamicResource AppBackgroundGradientBrush}">
    <ScrollViewer>
        <StackPanel Margin="20" Spacing="20">
            <!-- Buttons -->
            <StackPanel>
                <TextBlock Text="Buttons:" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="16" FontWeight="Bold" Margin="0,0,0,10"/>
                <Button Style="{StaticResource NeonPrimaryButtonStyle}" Content="Primary" Margin="0,0,10,0" />
                <Button Style="{StaticResource NeonSecondaryButtonStyle}" Content="Secondary" Margin="0,0,10,0" />
                <Button Style="{StaticResource NeonDangerButtonStyle}" Content="Danger" />
            </StackPanel>

            <!-- TextBox -->
            <StackPanel>
                <TextBlock Text="TextBox:" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="16" FontWeight="Bold" Margin="0,0,0,10"/>
                <TextBox Style="{StaticResource NeonTextBoxStyle}" Text="Type here..." Width="300" />
            </StackPanel>

            <!-- ListBox -->
            <StackPanel>
                <TextBlock Text="ListBox:" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="16" FontWeight="Bold" Margin="0,0,0,10"/>
                <ListBox Style="{StaticResource NeonListBoxStyle}"
                         ItemContainerStyle="{StaticResource NeonListBoxItemStyle}"
                         Height="150" Width="300">
                    <ListBoxItem Content="Item 1" />
                    <ListBoxItem Content="Item 2" />
                    <ListBoxItem Content="Item 3" />
                </ListBox>
            </StackPanel>

            <!-- TabControl -->
            <StackPanel>
                <TextBlock Text="TabControl:" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="16" FontWeight="Bold" Margin="0,0,0,10"/>
                <TabControl Style="{StaticResource NeonTabControlStyle}">
                    <TabItem Header="Tab 1" Style="{StaticResource NeonTabItemStyle}">
                        <TextBlock Text="Content 1" Margin="20" Foreground="{DynamicResource TextPrimaryBrush}" />
                    </TabItem>
                    <TabItem Header="Tab 2" Style="{StaticResource NeonTabItemStyle}">
                        <TextBlock Text="Content 2" Margin="20" Foreground="{DynamicResource TextPrimaryBrush}" />
                    </TabItem>
                </TabControl>
            </StackPanel>

            <!-- Card -->
            <StackPanel>
                <TextBlock Text="Card:" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="16" FontWeight="Bold" Margin="0,0,0,10"/>
                <Border Style="{StaticResource NeonCardStyle}" Width="300">
                    <StackPanel>
                        <TextBlock Text="Card Header" Foreground="{DynamicResource TextPrimaryBrush}" FontSize="14" FontWeight="Bold" />
                        <TextBlock Text="Card content goes here" Foreground="{DynamicResource TextSecondaryBrush}" Margin="0,10,0,0" />
                    </StackPanel>
                </Border>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</Window>
```

---

## 📝 Версия

Создано: Февраль 2026
Версия: 1.0
Совместимость: WPF 4.7.2+

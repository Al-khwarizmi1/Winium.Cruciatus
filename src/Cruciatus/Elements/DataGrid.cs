﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataGrid.cs" company="2GIS">
//   Cruciatus
// </copyright>
// <summary>
//   Представляет элемент управления таблица.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Cruciatus.Elements
{
    #region using

    using System;
    using System.Windows.Automation;

    using Cruciatus.Exceptions;
    using Cruciatus.Extensions;
    using Cruciatus.Interfaces;

    #endregion

    /// <summary>
    /// Представляет элемент управления таблица.
    /// </summary>
    public class DataGrid : CruciatusElement, IContainerElement
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="DataGrid"/>.
        /// </summary>
        public DataGrid()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="DataGrid"/>.
        /// </summary>
        /// <param name="parent">
        /// Элемент, являющийся родителем для таблицы.
        /// </param>
        /// <param name="automationId">
        /// Уникальный идентификатор таблицы.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Входные параметры не должны быть нулевыми.
        /// </exception>
        public DataGrid(AutomationElement parent, string automationId)
        {
            Initialize(parent, automationId);
        }

        /// <summary>
        /// Возвращает значение, указывающее, включена ли таблица.
        /// </summary>
        /// <exception cref="PropertyNotSupportedException">
        /// Таблица не поддерживает данное свойство.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// При получении значения свойства не удалось привести его к ожидаемому типу.
        /// </exception>
        public bool IsEnabled
        {
            get
            {
                return this.GetPropertyValue<bool>(AutomationElement.IsEnabledProperty);
            }
        }

        /// <summary>
        /// Возвращает количество строк в таблице.
        /// </summary>
        /// <exception cref="PropertyNotSupportedException">
        /// Таблица не поддерживает данное свойство.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// При получении значения свойства не удалось привести его к ожидаемому типу.
        /// </exception>
        public int RowCount
        {
            get
            {
                return this.GetPropertyValue<int>(GridPattern.RowCountProperty);
            }
        }

        /// <summary>
        /// Возвращает количество столбцов в таблице.
        /// </summary>
        /// <exception cref="PropertyNotSupportedException">
        /// Таблица не поддерживает данное свойство.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// При получении значения свойства не удалось привести его к ожидаемому типу.
        /// </exception>
        public int ColumnCount
        {
            get
            {
                return this.GetPropertyValue<int>(GridPattern.ColumnCountProperty);
            }
        }

        /// <summary>
        /// Возвращает текстовое представление имени класса.
        /// </summary>
        internal override string ClassName
        {
            get
            {
                return "DataGrid";
            }
        }

        internal override ControlType GetType
        {
            get
            {
                return ControlType.DataGrid;
            }
        }

        void IContainerElement.Initialize(AutomationElement parent, string automationId)
        {
            Initialize(parent, automationId);
        }

        /// <summary>
        /// Выполняет прокрутку до ячейки с указанным номером строки и колонки.
        /// </summary>
        /// <param name="row">
        /// Номер строки.
        /// </param>
        /// <param name="column">
        /// Номер колонки.
        /// </param>
        /// <returns>
        /// Значение true если прокрутить удалось либо в этом нет необходимости;
        /// в противном случае значение - false.
        /// </returns>
        public virtual bool ScrollTo(int row, int column)
        {
            // Проверка, что таблица включена
            var isEnabled = CruciatusFactory.WaitingValues(
                () => IsEnabled, 
                value => value != true);
            if (!isEnabled)
            {
                LastErrorMessage = string.Format("{0} отключена.", ToString());
                return false;
            }

            // Проверка на дурака
            if (row < 0 || column < 0)
            {
                LastErrorMessage = string.Format(
                    "В {0} ячейка [{1}, {2}] не существует, т.к. задан отрицательный номер.", 
                    ToString(), 
                    row, 
                    column);
                return false;
            }

            // Получение шаблона прокрутки у таблицы
            var scrollPattern = Element.GetCurrentPattern(ScrollPattern.Pattern) as ScrollPattern;
            if (scrollPattern == null)
            {
                LastErrorMessage = string.Format("{0} не поддерживает шаблон прокрутки.", ToString());
                return false;
            }

            // Условие для вертикального поиска ячейки [row, 0] (через строку)
            var cellCondition = new AndCondition(
                new PropertyCondition(AutomationElement.IsGridItemPatternAvailableProperty, true), 
                new PropertyCondition(GridItemPattern.RowProperty, row));

            // Стартовый поиск ячейки
            var cell = Element.FindFirst(TreeScope.Subtree, cellCondition);

            // Вертикальная прокрутка (при необходимости и возможности)
            if (cell == null && scrollPattern.Current.VerticallyScrollable)
            {
                // Установка самого верхнего положения прокрутки
                while (scrollPattern.Current.VerticalScrollPercent > 0.1)
                {
                    scrollPattern.ScrollVertical(ScrollAmount.LargeIncrement);
                }

                // Установка самого левого положения прокрутки (при возможности)
                if (scrollPattern.Current.HorizontallyScrollable)
                {
                    while (scrollPattern.Current.HorizontalScrollPercent > 0.1)
                    {
                        scrollPattern.ScrollHorizontal(ScrollAmount.LargeIncrement);
                    }
                }

                // Основная вертикальная прокрутка
                while (cell == null && scrollPattern.Current.VerticalScrollPercent < 99.9)
                {
                    scrollPattern.ScrollVertical(ScrollAmount.LargeIncrement);
                    cell = Element.FindFirst(TreeScope.Subtree, cellCondition);
                }
            }

            // Если прокрутив до конца ячейка не найдена, то номер строки не действительный
            if (cell == null)
            {
                LastErrorMessage = string.Format("В {0} нет строки с номером {1}.", ToString(), row);
                return false;
            }

            // Если точка клика ячейки [row, 0] под границей таблицы - докручиваем по вертикали вниз
            while (cell.ClickablePointUnder(Element, scrollPattern))
            {
                scrollPattern.ScrollVertical(ScrollAmount.SmallIncrement);
            }

            // Если точка клика ячейки [row, 0] над границей таблицы - докручиваем по вертикали вверх
            while (cell.ClickablePointOver(Element))
            {
                scrollPattern.ScrollVertical(ScrollAmount.SmallDecrement);
            }

            // Условие для горизонтального поиска ячейки [row, column]
            cellCondition = new AndCondition(
                new PropertyCondition(AutomationElement.IsGridItemPatternAvailableProperty, true), 
                new PropertyCondition(GridItemPattern.RowProperty, row), 
                new PropertyCondition(GridItemPattern.ColumnProperty, column));

            // Стартовый поиск ячейки
            cell = Element.FindFirst(TreeScope.Subtree, cellCondition);

            // Основная горизонтальная прокрутка (при необходимости и возможности)
            if (cell == null && scrollPattern.Current.HorizontallyScrollable)
            {
                while (cell == null && scrollPattern.Current.HorizontalScrollPercent < 99.9)
                {
                    scrollPattern.ScrollHorizontal(ScrollAmount.LargeIncrement);
                    cell = Element.FindFirst(TreeScope.Subtree, cellCondition);
                }
            }

            // Если прокрутив до конца ячейка не найдена, то номер колонки не действительный
            if (cell == null)
            {
                LastErrorMessage = string.Format("В {0} нет колонки с номером {1}.", ToString(), column);
                return false;
            }

            // Если точка клика ячейки [row, column] справа от границы таблицы - докручиваем по горизонтали вправо
            while (cell.ClickablePointRight(Element, scrollPattern))
            {
                scrollPattern.ScrollHorizontal(ScrollAmount.SmallIncrement);
            }

            // Если точка клика ячейки [row, column] слева от границы таблицы - докручиваем по горизонтали влево
            while (cell.ClickablePointLeft(Element))
            {
                scrollPattern.ScrollHorizontal(ScrollAmount.SmallDecrement);
            }

            return true;
        }

        /// <summary>
        /// Выбирает ячейку с указанным номером строки и колонки.
        /// </summary>
        /// <param name="row">
        /// Номер строки.
        /// </param>
        /// <param name="column">
        /// Номер колонки.
        /// </param>
        /// <returns>
        /// Значение true если выбрать удалось; в противном случае значение - false.
        /// </returns>
        public virtual bool SelectCell(int row, int column)
        {
            var cell = Item<ClickableElement>(row, column);
            if (cell == null)
            {
                return false;
            }

            var result = cell.Click();
            if (result)
            {
                return true;
            }

            LastErrorMessage = cell.LastErrorMessage;
            return false;
        }

        /// <summary>
        /// Возвращает элемент заданного типа с указанным номером строки и колонки.
        /// </summary>
        /// <param name="row">
        /// Номер строки.
        /// </param>
        /// <param name="column">
        /// Номер колонки.
        /// </param>
        /// <typeparam name="T">
        /// Тип элемента.
        /// </typeparam>
        /// <returns>
        /// Искомый элемент, либо null, если найти не удалось.
        /// </returns>
        public virtual T Item<T>(int row, int column) where T : CruciatusElement, IListElement, new()
        {
            // Проверка, что таблица включена
            var isEnabled = CruciatusFactory.WaitingValues(
                () => IsEnabled, 
                value => value != true);
            if (!isEnabled)
            {
                LastErrorMessage = string.Format("{0} отключена.", ToString());
                return null;
            }

            // Проверка на дурака
            if (row < 0 || column < 0)
            {
                LastErrorMessage = string.Format(
                    "В {0} ячейка [{1}, {2}] не существует, т.к. задан отрицательный номер.", 
                    ToString(), 
                    row, 
                    column);
                return null;
            }

            // Условие для поиска ячейки [row, column]
            var cellCondition = new AndCondition(
                new PropertyCondition(AutomationElement.IsGridItemPatternAvailableProperty, true), 
                new PropertyCondition(GridItemPattern.RowProperty, row), 
                new PropertyCondition(GridItemPattern.ColumnProperty, column));
            var cell = Element.FindFirst(TreeScope.Subtree, cellCondition);

            // Проверка, что ячейку видно
            if (cell == null || !Element.ContainsClickablePoint(cell))
            {
                LastErrorMessage = string.Format(
                    "В {0} ячейка [{1}, {2}] вне видимости или не существует.", 
                    ToString(), 
                    row, 
                    column);
                return null;
            }

            // Поиск подходящего элемента в ячейке
            var item = new T();
            var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, item.GetType);
            var elem = cell.FindFirst(TreeScope.Subtree, condition);
            if (elem == null)
            {
                LastErrorMessage = string.Format(
                    "В {0}, ячейка [{1}, {2}], нет элемента желаемого типа.", 
                    ToString(), 
                    row, 
                    column);
                return null;
            }

            item.Initialize(elem);
            return item;
        }
    }
}
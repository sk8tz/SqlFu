﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using CavemanTools.Logging;
using SqlFu.Internals;

namespace SqlFu.Expressions
{
    public class ExpressionSqlBuilder<T>
    {
        private readonly IDbProviderExpressionHelper _provider;
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly ParametersManager _pm = new ParametersManager();
        private readonly ExpressionWriter _writer;
        private readonly TableInfo _ti;

        public ExpressionSqlBuilder(IDbProviderExpressionHelper provider)
        {
            _provider = provider;
            _ti = TableInfo.ForType(typeof (T));
            provider.MustNotBeNull();
            _writer = new ExpressionWriter(_sb, provider, _pm);
        }

        public ExpressionSqlBuilder<T> WriteSelect()
        {
            _sb.Append("select ");
            return this;
        }

        public ExpressionSqlBuilder<T> WriteFrom()
        {
            _sb.AppendFormat(" from {0}", _provider.EscapeName(_ti.Name));
            return this;
        }

        private static string SortToString(SortOrder sort)
        {
            return sort == SortOrder.Descending ? "desc" : "asc";
        }

        public ExpressionSqlBuilder<T> OrderBy(Expression<Func<T, object>> selector,
                                               SortOrder sort = SortOrder.Ascending)
        {
            if (_order)
            {
                _sb.Append(", ");
            }
            else
            {
                _sb.Append(" order by ");
            }

            var column = selector.Body.GetPropertyName();
            _sb.AppendFormat("{0} {1}", _provider.EscapeName(column), SortToString(sort));
            _order = true;
            return this;
        }

        public ExpressionSqlBuilder<T> WriteSelectAllColumns()
        {
            //_sb.Append(string.Join(",", _ti.Columns/*.Except(_ti.Excludes)*/.Select(x => _provider.EscapeName(x))));
            _sb.Append(string.Join(",", _ti.Columns.Select(x => _provider.EscapeName(x))));

            return this;
        }

        public ExpressionSqlBuilder<T> WriteSelectColumn<R>(Expression<Func<T, R>> selector, string alias = null)
        {
            var name = selector.Body.GetPropertyName();
            if (_hasColumn)
            {
                _sb.Append(", ");
            }
            _sb.Append(_provider.EscapeName(name));
            _hasColumn = true;
            if (alias != null)
            {
                _sb.AppendFormat(" as {0}", alias);
            }

            return this;
        }

        public ExpressionSqlBuilder<T> WriteTableName()
        {
            _sb.Append(_provider.EscapeName(_ti.Name));
            return this;
        }

        public ExpressionSqlBuilder<T> Append(string text)
        {
            _sb.Append(text);
            return this;
        }

        /// <summary>
        /// Removes comma after columns enumeration
        /// </summary>
        /// <returns></returns>
        public ExpressionSqlBuilder<T> EndEnumeration()
        {
            _sb.RemoveLastIfEquals(",");
            return this;
        }

        private bool _where;
        private bool _hasColumn;
        private bool _order;

        public ExpressionSqlBuilder<T> Where(Expression<Func<T, bool>> criteria)
        {
            if (!_where)
            {
                _sb.Append(" where ");
                _where = true;
            }
            else
            {
                _sb.Append(" and ");
            }
            Write(criteria);
            return this;
        }

        public ExpressionSqlBuilder<T> Write(Expression<Func<T, bool>> criteria)
        {
            _writer.Write(criteria);
            return this;
        }

        public ExpressionSqlBuilder<T> Write(Expression<Func<T, object>> statement)
        {
            _writer.Write(statement);
            return this;
        }

        public ParametersManager Parameters
        {
            get { return _pm; }
        }

        //public TableInfo TableInfo
        //{
        //    get { return _ti; }
        //}

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
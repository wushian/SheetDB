﻿namespace SheetDB.Implementation
{
    using Helpers;
    using Newtonsoft.Json;
    using System.Linq;
    using System.Net;
    using Transport;

    public class Database : IDatabase
    {
        private readonly IConnector _connector;

        private readonly string _spreadsheetId;

        public Database(IConnector connector, string spreadsheetId)
        {
            this._connector = connector;
            this._spreadsheetId = spreadsheetId;
        }

        public ITable<T> CreateTable<T>(string name) where T : new()
        {
            var uri = string.Format("https://sheets.googleapis.com/v4/spreadsheets/{0}:batchUpdate", this._spreadsheetId);

            var fields = Utils.GetFields<T>();

            var request = this._connector.CreateRequest(uri);

            var sheetId = "1111";

            var payload = JsonConvert.SerializeObject(new
            {
                requests = new[] {
                    new {
                        addSheet = new
                        {
                            properties = new
                            {
                                sheetId = sheetId,
                                title = name,
                                sheetType = "GRID",
                                gridProperties = new
                                {
                                    rowCount = 1,
                                    columnCount = fields.Count()
                                }
                            }
                        },
                        appendCells = new
                        {
                            sheetId = sheetId,
                            rows = new[] {
                                new {
                                    values = new[] { fields.Select(a => new { userEnteredValue = new { stringValue = a.Name.ToLowerInvariant() } }) }
                                }
                            },
                            fields = "*"
                        }
                    }
                }
            });

            var response = new ResponseValidator(this._connector.Send(request, HttpMethod.Post, payload));

            dynamic data = response
               .Status(HttpStatusCode.OK)
               .Response.Data<dynamic>();

            return new Table<T>(this._connector, this._spreadsheetId, sheetId);
        }

        public void Delete()
        {
            var uri = string.Format("https://www.googleapis.com/drive/v3/files/{0}", this._spreadsheetId);

            var request = this._connector.CreateRequest(uri);

            var response = new ResponseValidator(this._connector.Send(request, HttpMethod.Delete));

            response
               .Status(HttpStatusCode.NoContent);
        }
    }
}

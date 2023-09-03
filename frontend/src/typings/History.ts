import ModelBase from 'App/ModelBase';

export interface HistoryData {
  source: string;
  host: string;
  limit: number;
  offset: number;
  elapsedTime: number;
  query: string;
  queryType: string;
}

interface History extends ModelBase {
  indexerId: number;
  date: string;
  successful: boolean;
  eventType: string;
  data: HistoryData;
}

export default History;

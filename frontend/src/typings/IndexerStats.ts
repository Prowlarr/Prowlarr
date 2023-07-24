export interface IndexerStatsIndexer {
  indexerId: number;
  indexerName: string;
  averageResponseTime: number;
  numberOfQueries: number;
  numberOfGrabs: number;
  numberOfRssQueries: number;
  numberOfAuthQueries: number;
  numberOfFailedQueries: number;
  numberOfFailedGrabs: number;
  numberOfFailedRssQueries: number;
  numberOfFailedAuthQueries: number;
}

export interface IndexerStatsUserAgent {
  userAgent: string;
  numberOfQueries: number;
  numberOfGrabs: number;
}

export interface IndexerStatsHost {
  host: string;
  numberOfQueries: number;
  numberOfGrabs: number;
}

export interface IndexerStats {
  indexers: IndexerStatsIndexer[];
  userAgents: IndexerStatsUserAgent[];
  hosts: IndexerStatsHost[];
}

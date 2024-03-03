import React, { useCallback } from 'react';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRowButton from 'Components/Table/TableRowButton';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';
import { icons } from 'Helpers/Props';
import CapabilitiesLabel from 'Indexer/Index/Table/CapabilitiesLabel';
import PrivacyLabel from 'Indexer/Index/Table/PrivacyLabel';
import ProtocolLabel from 'Indexer/Index/Table/ProtocolLabel';
import { IndexerCapabilities, IndexerPrivacy } from 'Indexer/Indexer';
import translate from 'Utilities/String/translate';
import styles from './SelectIndexerRow.css';

interface SelectIndexerRowProps {
  name: string;
  protocol: DownloadProtocol;
  privacy: IndexerPrivacy;
  language: string;
  description: string;
  capabilities: IndexerCapabilities;
  implementation: string;
  implementationName: string;
  isExistingIndexer: boolean;
  onIndexerSelect(...args: unknown[]): void;
}

function SelectIndexerRow(props: SelectIndexerRowProps) {
  const {
    name,
    protocol,
    privacy,
    language,
    description,
    capabilities,
    implementation,
    implementationName,
    isExistingIndexer,
    onIndexerSelect,
  } = props;

  const onPress = useCallback(() => {
    onIndexerSelect({ implementation, implementationName, name });
  }, [implementation, implementationName, name, onIndexerSelect]);

  return (
    <TableRowButton onPress={onPress}>
      <TableRowCell className={styles.protocol}>
        <ProtocolLabel protocol={protocol} />
      </TableRowCell>

      <TableRowCell>
        {name}
        {isExistingIndexer ? (
          <Icon
            className={styles.alreadyExistsIcon}
            name={icons.CHECK_CIRCLE}
            size={15}
            title={translate('IndexerAlreadySetup')}
          />
        ) : null}
      </TableRowCell>

      <TableRowCell>{language}</TableRowCell>

      <TableRowCell>{description}</TableRowCell>

      <TableRowCell>
        <PrivacyLabel privacy={privacy} />
      </TableRowCell>

      <TableRowCell>
        <CapabilitiesLabel capabilities={capabilities} />
      </TableRowCell>
    </TableRowButton>
  );
}

export default SelectIndexerRow;

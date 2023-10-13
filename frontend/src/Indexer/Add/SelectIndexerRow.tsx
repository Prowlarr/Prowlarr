import React, { useCallback } from 'react';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRowButton from 'Components/Table/TableRowButton';
import { icons } from 'Helpers/Props';
import CapabilitiesLabel from 'Indexer/Index/Table/CapabilitiesLabel';
import ProtocolLabel from 'Indexer/Index/Table/ProtocolLabel';
import { IndexerCapabilities } from 'Indexer/Indexer';
import firstCharToUpper from 'Utilities/String/firstCharToUpper';
import translate from 'Utilities/String/translate';
import styles from './SelectIndexerRow.css';

interface SelectIndexerRowProps {
  name: string;
  protocol: string;
  privacy: string;
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

      <TableRowCell>{translate(firstCharToUpper(privacy))}</TableRowCell>

      <TableRowCell>
        <CapabilitiesLabel capabilities={capabilities} />
      </TableRowCell>
    </TableRowButton>
  );
}

export default SelectIndexerRow;
